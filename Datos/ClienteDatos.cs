using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace Proyecto_facturas.Datos
{
    internal static class ClienteDatos
    {
        private const string Duplicado = "Ya existe un cliente con ese nombre o correo electrónico.";
        private const string Relacion = "No se puede eliminar: el cliente tiene facturas.";

        public static DataTable Listar(string filtro)
        {
            filtro = (filtro ?? "").Trim();
            string patron = Bd.Contiene(filtro);
            return Bd.Consulta(
                "SELECT Idcliente, Nombre, Telefono, Email, Direccion FROM Cliente " +
                "WHERE Nombre LIKE @f OR Email LIKE @f OR Telefono LIKE @f OR CAST(Idcliente AS CHAR) = @i ORDER BY Idcliente",
                Bd.P("@f", patron), Bd.P("@i", filtro));
        }

        // Nombres para los combos (Factura.Cliente guarda el nombre)
        public static DataTable Nombres()
        {
            return Bd.Consulta("SELECT Nombre FROM Cliente ORDER BY Nombre");
        }

        public static string Insertar(string nombre, string telefono, string email, string direccion)
        {
            return Bd.Escribir("INSERT INTO Cliente (Nombre, Telefono, Email, Direccion) VALUES (@n, @t, @e, @d)",
                Duplicado, Relacion, false, Bd.P("@n", nombre), Bd.P("@t", telefono), Bd.P("@e", Nulo(email)), Bd.P("@d", Nulo(direccion)));
        }

        // Si cambia el nombre, tambien se actualiza en las facturas ya emitidas (Factura.Cliente es texto)
        public static string Actualizar(int id, string nombre, string telefono, string email, string direccion)
        {
            try
            {
                using (MySqlConnection cn = Conexion.Crear())
                {
                    cn.Open();
                    using (MySqlTransaction tx = cn.BeginTransaction())
                    {
                        string anterior;
                        using (MySqlCommand cmd = new MySqlCommand("SELECT Nombre FROM Cliente WHERE Idcliente = @id FOR UPDATE", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            anterior = cmd.ExecuteScalar() as string;
                        }
                        if (anterior == null) return "El registro ya no existe. Actualiza la lista.";

                        using (MySqlCommand cmd = new MySqlCommand(
                            "UPDATE Cliente SET Nombre = @n, Telefono = @t, Email = @e, Direccion = @d WHERE Idcliente = @id", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@n", nombre);
                            cmd.Parameters.AddWithValue("@t", telefono);
                            cmd.Parameters.AddWithValue("@e", Nulo(email));
                            cmd.Parameters.AddWithValue("@d", Nulo(direccion));
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        if (!string.Equals(anterior, nombre, StringComparison.Ordinal))
                            using (MySqlCommand cmd = new MySqlCommand("UPDATE Factura SET Cliente = @nuevo WHERE Cliente = @viejo", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@nuevo", nombre);
                                cmd.Parameters.AddWithValue("@viejo", anterior);
                                cmd.ExecuteNonQuery();
                            }

                        tx.Commit();
                        return null;
                    }
                }
            }
            catch (MySqlException ex)
            {
                string mensaje = Bd.Traducir(ex, Duplicado, Relacion);
                if (mensaje == null) throw;
                return mensaje;
            }
        }

        // No se elimina un cliente que ya tiene facturas (la factura guarda su nombre)
        public static string Eliminar(int id)
        {
            object nombre = Bd.Escalar("SELECT Nombre FROM Cliente WHERE Idcliente = @id", Bd.P("@id", id));
            if (nombre == null) return "El registro ya no existe. Actualiza la lista.";

            long facturas = Convert.ToInt64(Bd.Escalar("SELECT COUNT(*) FROM Factura WHERE Cliente = @n", Bd.P("@n", nombre)));
            if (facturas > 0) return Relacion;

            return Bd.Escribir("DELETE FROM Cliente WHERE Idcliente = @id", Duplicado, Relacion, true, Bd.P("@id", id));
        }

        private static object Nulo(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
    }
}
