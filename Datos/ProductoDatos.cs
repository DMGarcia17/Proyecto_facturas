using System.Data;

namespace Proyecto_facturas.Datos
{
    internal static class ProductoDatos
    {
        private const string Duplicado = "Ya existe un producto con esa descripción.";
        private const string Relacion = "No se puede eliminar: el producto aparece en facturas.";

        public static DataTable Listar(string filtro)
        {
            filtro = (filtro ?? "").Trim();
            return Bd.Consulta(
                "SELECT Idproducto, Descripcion, Cantidad, Precio FROM Producto " +
                "WHERE Descripcion LIKE @f OR CAST(Idproducto AS CHAR) = @i ORDER BY Idproducto",
                Bd.P("@f", Bd.Contiene(filtro)), Bd.P("@i", filtro));
        }

        // Productos con existencias para vender; incluye una columna Texto para el combo
        public static DataTable Disponibles()
        {
            DataTable t = Bd.Consulta("SELECT Idproducto, Descripcion, Cantidad, Precio FROM Producto WHERE Cantidad > 0 ORDER BY Descripcion");
            t.Columns.Add("Texto", typeof(string));
            foreach (DataRow f in t.Rows)
                f["Texto"] = string.Format("{0}  —  {1:C2}  (disponibles: {2})", f["Descripcion"], f["Precio"], f["Cantidad"]);
            return t;
        }

        public static string Insertar(string descripcion, int cantidad, double precio)
        {
            return Bd.Escribir("INSERT INTO Producto (Descripcion, Cantidad, Precio) VALUES (@d, @c, @p)",
                Duplicado, Relacion, false, Bd.P("@d", descripcion), Bd.P("@c", cantidad), Bd.P("@p", precio));
        }

        public static string Actualizar(int id, string descripcion, int cantidad, double precio)
        {
            return Bd.Escribir("UPDATE Producto SET Descripcion = @d, Cantidad = @c, Precio = @p WHERE Idproducto = @id",
                Duplicado, Relacion, true, Bd.P("@d", descripcion), Bd.P("@c", cantidad), Bd.P("@p", precio), Bd.P("@id", id));
        }

        public static string Eliminar(int id)
        {
            return Bd.Escribir("DELETE FROM Producto WHERE Idproducto = @id", Duplicado, Relacion, true, Bd.P("@id", id));
        }
    }
}
