using System.Data;
using MySql.Data.MySqlClient;

namespace Proyecto_facturas.Datos
{
    // Utilidades comunes de acceso a datos. Las escrituras devuelven null si todo salio bien
    // o el mensaje de negocio (duplicado, registro relacionado, regla de la base).
    internal static class Bd
    {
        private const int ErrorDuplicado = 1062;
        private const int ErrorRelacion = 1451;
        private const int ErrorCheck = 3819;

        public static MySqlParameter P(string nombre, object valor)
        {
            return new MySqlParameter(nombre, valor ?? System.DBNull.Value);
        }

        public static DataTable Consulta(string sql, params MySqlParameter[] parametros)
        {
            using (MySqlConnection cn = Conexion.Crear())
            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, cn))
            {
                da.SelectCommand.Parameters.AddRange(parametros);
                DataTable tabla = new DataTable();
                da.Fill(tabla);
                return tabla;
            }
        }

        public static object Escalar(string sql, params MySqlParameter[] parametros)
        {
            using (MySqlConnection cn = Conexion.Crear())
            using (MySqlCommand cmd = new MySqlCommand(sql, cn))
            {
                cmd.Parameters.AddRange(parametros);
                cn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // Ejecuta INSERT/UPDATE/DELETE. Si es UPDATE/DELETE y no afecta filas, el registro ya no existe.
        public static string Escribir(string sql, string mensajeDuplicado, string mensajeRelacion,
            bool debeExistir, params MySqlParameter[] parametros)
        {
            try
            {
                using (MySqlConnection cn = Conexion.Crear())
                using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                {
                    cmd.Parameters.AddRange(parametros);
                    cn.Open();
                    int filas = cmd.ExecuteNonQuery();
                    return debeExistir && filas == 0 ? "El registro ya no existe. Actualiza la lista." : null;
                }
            }
            catch (MySqlException ex)
            {
                string mensaje = Traducir(ex, mensajeDuplicado, mensajeRelacion);
                if (mensaje == null) throw;
                return mensaje;
            }
        }

        public static string Traducir(MySqlException ex, string mensajeDuplicado, string mensajeRelacion)
        {
            if (ex.Number == ErrorDuplicado) return mensajeDuplicado;
            if (ex.Number == ErrorRelacion) return mensajeRelacion;
            if (ex.Number == ErrorCheck) return "Los datos no cumplen las reglas de la base de datos.";
            return null;
        }

        // Patron LIKE que contiene el texto (escapa los comodines)
        public static string Contiene(string filtro)
        {
            filtro = (filtro ?? "").Trim();
            return "%" + filtro.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        }
    }
}
