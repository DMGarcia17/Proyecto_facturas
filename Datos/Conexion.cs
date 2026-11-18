using System;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Configuracion;

namespace Proyecto_facturas.Datos
{
    // Crea conexiones a MySQL con los datos del archivo .env
    internal static class Conexion
    {
        // Porcentaje de IVA (por ejemplo 13 = 13%)
        public static double IvaPorcentaje
        {
            get
            {
                double iva = EnvLoader.ObtenerDecimal("IVA_PORCENTAJE", 13);
                return iva < 0 || iva > 100 ? 13 : iva;
            }
        }

        public static string NombreBD
        {
            get
            {
                string nombre = EnvLoader.Requerido("DB_NAME");
                if (!Regex.IsMatch(nombre, @"^[A-Za-z0-9_]{1,64}$"))
                    throw new InvalidOperationException("DB_NAME solo puede tener letras, números y guion bajo (máximo 64).");
                return nombre;
            }
        }

        // Conexion a la base de datos de la aplicacion
        public static MySqlConnection Crear()
        {
            return new MySqlConnection(Cadena(true));
        }

        // Conexion al servidor sin elegir base (para crearla en una instalacion limpia)
        public static MySqlConnection CrearSinBase()
        {
            return new MySqlConnection(Cadena(false));
        }

        private static string Cadena(bool conBase)
        {
            if (!uint.TryParse(EnvLoader.Obtener("DB_PORT", "3306"), out uint puerto))
                throw new InvalidOperationException("DB_PORT debe ser un número.");
            if (!Enum.TryParse(EnvLoader.Obtener("DB_SSL_MODE", "Required"), true, out MySqlSslMode ssl))
                ssl = MySqlSslMode.Required;

            MySqlConnectionStringBuilder b = new MySqlConnectionStringBuilder
            {
                Server = EnvLoader.Requerido("DB_HOST"),
                Port = puerto,
                UserID = EnvLoader.Requerido("DB_USER"),
                Password = EnvLoader.Requerido("DB_PASSWORD"),
                SslMode = ssl,
                ConnectionTimeout = 15,
                CharacterSet = "utf8mb4"
            };
            if (conBase) b.Database = NombreBD;
            return b.ConnectionString;
        }
    }
}
