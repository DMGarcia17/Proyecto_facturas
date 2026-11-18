using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Configuracion;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Datos
{
    // "Cargador" de la base de datos: en un servidor limpio crea la base, las tablas y los datos
    // iniciales (usuario administrador y ejemplos). Es idempotente: si ya existe algo, no lo toca.
    internal static class InicializadorBD
    {
        public static void Ejecutar()
        {
            CrearBase();

            using (MySqlConnection cn = Conexion.Crear())
            {
                cn.Open();
                CrearTablas(cn);
                CargarAdministrador(cn);
                CargarProductosEjemplo(cn);
                CargarClientesEjemplo(cn);
            }
        }

        private static void CrearBase()
        {
            try
            {
                using (MySqlConnection cn = Conexion.CrearSinBase())
                {
                    cn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "CREATE DATABASE IF NOT EXISTS `" + Conexion.NombreBD + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", cn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex) when (ex.Number == 1044 || ex.Number == 1227)
            {
                // Sin permiso para crear bases: se sigue, y falla despues solo si la base no existe
            }
        }

        // Ejecuta sql\01_schema.sql (recurso embebido), sentencia por sentencia
        private static void CrearTablas(MySqlConnection cn)
        {
            Assembly ensamblado = Assembly.GetExecutingAssembly();
            string recurso = ensamblado.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("01_schema.sql", StringComparison.OrdinalIgnoreCase));
            if (recurso == null) throw new InvalidOperationException("No se encontró el esquema SQL embebido.");

            string script;
            using (StreamReader lector = new StreamReader(ensamblado.GetManifestResourceStream(recurso), Encoding.UTF8))
                script = lector.ReadToEnd();

            // Se quitan los comentarios de linea y se separa por ';'
            StringBuilder limpio = new StringBuilder();
            foreach (string linea in script.Split('\n'))
                if (!linea.TrimStart().StartsWith("--")) limpio.AppendLine(linea);

            foreach (string sentencia in limpio.ToString().Split(';'))
            {
                if (string.IsNullOrWhiteSpace(sentencia)) continue;
                using (MySqlCommand cmd = new MySqlCommand(sentencia, cn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static void CargarAdministrador(MySqlConnection cn)
        {
            if (Contar(cn, "Usuario") > 0) return;

            string usuario = EnvLoader.Obtener("ADMIN_USER", "admin");
            string clave = EnvLoader.Obtener("ADMIN_PASSWORD", "Admin123*");
            string nombre = EnvLoader.Obtener("ADMIN_FULLNAME", "Administrador del sistema");
            Seguridad.CrearHash(clave, out string hash, out string sal);

            using (MySqlCommand cmd = new MySqlCommand(
                "INSERT INTO Usuario (Nombre, NombreCompleto, PasswordHash, PasswordSalt, FechaRegistro) VALUES (@n, @c, @h, @s, @f)", cn))
            {
                cmd.Parameters.AddWithValue("@f", DateTime.Now); // hora local, no la del servidor
                cmd.Parameters.AddWithValue("@n", usuario);
                cmd.Parameters.AddWithValue("@c", nombre);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@s", sal);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CargarProductosEjemplo(MySqlConnection cn)
        {
            if (Contar(cn, "Producto") > 0) return;

            object[,] datos =
            {
                { "Laptop 15 pulgadas", 12, 649.99 },
                { "Mouse inalámbrico", 85, 12.50 },
                { "Teclado mecánico", 40, 45.00 },
                { "Monitor 24 pulgadas", 18, 139.90 },
                { "Impresora multifuncional", 8, 189.00 },
                { "Cable HDMI 2m", 60, 6.75 }
            };

            for (int i = 0; i < datos.GetLength(0); i++)
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Producto (Descripcion, Cantidad, Precio) VALUES (@d, @c, @p)", cn))
                {
                    cmd.Parameters.AddWithValue("@d", datos[i, 0]);
                    cmd.Parameters.AddWithValue("@c", datos[i, 1]);
                    cmd.Parameters.AddWithValue("@p", datos[i, 2]);
                    cmd.ExecuteNonQuery();
                }
        }

        private static void CargarClientesEjemplo(MySqlConnection cn)
        {
            if (Contar(cn, "Cliente") > 0) return;

            string[,] datos =
            {
                { "Consumidor final", "2200-0000", null, "San Salvador" },
                { "Comercial El Sol, S.A. de C.V.", "2222-3344", "ventas@elsol.example.com", "Colonia Escalón, San Salvador" },
                { "María López", "7123-4567", "maria.lopez@example.com", "Santa Tecla, La Libertad" }
            };

            for (int i = 0; i < datos.GetLength(0); i++)
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Cliente (Nombre, Telefono, Email, Direccion) VALUES (@n, @t, @e, @d)", cn))
                {
                    cmd.Parameters.AddWithValue("@n", datos[i, 0]);
                    cmd.Parameters.AddWithValue("@t", datos[i, 1]);
                    cmd.Parameters.AddWithValue("@e", (object)datos[i, 2] ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@d", datos[i, 3]);
                    cmd.ExecuteNonQuery();
                }
        }

        private static long Contar(MySqlConnection cn, string tabla)
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM " + tabla, cn))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }
    }
}
