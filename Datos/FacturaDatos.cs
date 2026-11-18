using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace Proyecto_facturas.Datos
{
    // Producto y cantidad elegidos en una factura nueva
    internal class LineaFactura
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }

    internal static class FacturaDatos
    {
        // Facturas en un rango de fechas (inclusive), con filtro opcional por cliente o numero
        public static DataTable Listar(DateTime desde, DateTime hasta, string filtro)
        {
            filtro = (filtro ?? "").Trim();
            return Bd.Consulta(
                "SELECT f.Idfactura, f.Cliente, f.Fecha, " +
                "(SELECT COALESCE(SUM(d.cantidad), 0) FROM Detallefactura d WHERE d.idfactura = f.Idfactura) AS Articulos, " +
                "f.Subtotal, f.IVA, f.Total FROM Factura f " +
                "WHERE f.Fecha >= @d AND f.Fecha < @h AND (f.Cliente LIKE @f OR CAST(f.Idfactura AS CHAR) = @i) " +
                "ORDER BY f.Fecha DESC, f.Idfactura DESC",
                Bd.P("@d", desde.Date), Bd.P("@h", hasta.Date.AddDays(1)), Bd.P("@f", Bd.Contiene(filtro)), Bd.P("@i", filtro));
        }

        public static DataTable Detalle(int idFactura)
        {
            return Bd.Consulta(
                "SELECT d.Iddetalle, p.Descripcion, d.cantidad, d.Precio, d.Total FROM Detallefactura d " +
                "JOIN Producto p ON p.Idproducto = d.idproducto WHERE d.idfactura = @id ORDER BY d.Iddetalle",
                Bd.P("@id", idFactura));
        }

        // Crea la factura y su detalle de forma atomica y descuenta las existencias.
        // Los precios y totales se toman de la base de datos, no de la pantalla.
        // Devuelve null si todo salio bien o el mensaje del problema.
        public static string Crear(string cliente, IEnumerable<LineaFactura> lineas, out int idFactura)
        {
            idFactura = 0;

            // Une lineas repetidas del mismo producto
            List<LineaFactura> unidas = lineas.GroupBy(l => l.IdProducto)
                .Select(g => new LineaFactura { IdProducto = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
                .OrderBy(l => l.IdProducto).ToList();
            if (unidas.Count == 0) return "Agrega al menos un producto a la factura.";

            double iva = Conexion.IvaPorcentaje;

            using (MySqlConnection cn = Conexion.Crear())
            {
                cn.Open();
                using (MySqlTransaction tx = cn.BeginTransaction())
                {
                    List<double[]> detalles = new List<double[]>(); // {idProducto, cantidad, precio, total}
                    double subtotal = 0;

                    foreach (LineaFactura l in unidas)
                    {
                        if (l.Cantidad <= 0) return "La cantidad de cada producto debe ser mayor a cero.";

                        string descripcion; int stock; double precio;
                        using (MySqlCommand cmd = new MySqlCommand(
                            "SELECT Descripcion, Cantidad, Precio FROM Producto WHERE Idproducto = @id FOR UPDATE", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", l.IdProducto);
                            using (MySqlDataReader r = cmd.ExecuteReader())
                            {
                                if (!r.Read()) return "Uno de los productos ya no existe. Actualiza la lista.";
                                descripcion = r.GetString(0);
                                stock = r.GetInt32(1);
                                precio = r.GetDouble(2);
                            }
                        }

                        if (stock < l.Cantidad)
                            return "Existencias insuficientes de '" + descripcion + "': disponibles " + stock + ".";

                        using (MySqlCommand cmd = new MySqlCommand("UPDATE Producto SET Cantidad = Cantidad - @c WHERE Idproducto = @id", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@c", l.Cantidad);
                            cmd.Parameters.AddWithValue("@id", l.IdProducto);
                            cmd.ExecuteNonQuery();
                        }

                        double total = Redondear(precio * l.Cantidad);
                        subtotal += total;
                        detalles.Add(new[] { l.IdProducto, l.Cantidad, precio, total });
                    }

                    subtotal = Redondear(subtotal);
                    double montoIva = Redondear(subtotal * iva / 100.0);
                    double totalFactura = Redondear(subtotal + montoIva);

                    using (MySqlCommand cmd = new MySqlCommand(
                        "INSERT INTO Factura (Cliente, Fecha, Subtotal, IVA, Total) VALUES (@c, @f, @s, @i, @t)", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@c", cliente);
                        cmd.Parameters.AddWithValue("@f", DateTime.Now);
                        cmd.Parameters.AddWithValue("@s", subtotal);
                        cmd.Parameters.AddWithValue("@i", montoIva);
                        cmd.Parameters.AddWithValue("@t", totalFactura);
                        cmd.ExecuteNonQuery();
                        idFactura = (int)cmd.LastInsertedId;
                    }

                    foreach (double[] d in detalles)
                        using (MySqlCommand cmd = new MySqlCommand(
                            "INSERT INTO Detallefactura (idfactura, idproducto, cantidad, Precio, Total) VALUES (@f, @p, @c, @pr, @t)", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@f", idFactura);
                            cmd.Parameters.AddWithValue("@p", (int)d[0]);
                            cmd.Parameters.AddWithValue("@c", (int)d[1]);
                            cmd.Parameters.AddWithValue("@pr", d[2]);
                            cmd.Parameters.AddWithValue("@t", d[3]);
                            cmd.ExecuteNonQuery();
                        }

                    tx.Commit();
                    return null;
                }
            }
        }

        // Elimina la factura con su detalle y devuelve las existencias al inventario
        public static string Eliminar(int idFactura)
        {
            using (MySqlConnection cn = Conexion.Crear())
            {
                cn.Open();
                using (MySqlTransaction tx = cn.BeginTransaction())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Factura WHERE Idfactura = @id FOR UPDATE", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idFactura);
                        if (Convert.ToInt64(cmd.ExecuteScalar()) == 0) return "La factura ya no existe. Actualiza la lista.";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(
                        "UPDATE Producto p JOIN Detallefactura d ON d.idproducto = p.Idproducto " +
                        "SET p.Cantidad = p.Cantidad + d.cantidad WHERE d.idfactura = @id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idFactura);
                        cmd.ExecuteNonQuery();
                    }

                    // El detalle se borra solo (ON DELETE CASCADE)
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Factura WHERE Idfactura = @id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", idFactura);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return null;
                }
            }
        }

        private static double Redondear(double valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }
    }
}
