using System;
using System.Data;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Datos
{
    internal static class UsuarioDatos
    {
        private const string Duplicado = "Ya existe un usuario con ese nombre de usuario.";
        private const string Relacion = "No se puede eliminar el usuario.";

        // Devuelve la fila del usuario si las credenciales son correctas; si no, null
        public static DataRow Autenticar(string nombre, string contrasena)
        {
            DataTable t = Bd.Consulta(
                "SELECT IdUsuario, Nombre, NombreCompleto, PasswordHash, PasswordSalt FROM Usuario WHERE Nombre = @n",
                Bd.P("@n", nombre));

            if (t.Rows.Count == 0)
            {
                // Se hace el mismo trabajo que con un usuario real para no revelar si existe
                Seguridad.Verificar(contrasena, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", "AAAAAAAAAAAAAAAAAAAAAA==");
                return null;
            }

            DataRow fila = t.Rows[0];
            // MySQL compara sin distinguir mayusculas; se exige coincidencia exacta del nombre
            if (!string.Equals((string)fila["Nombre"], nombre, StringComparison.Ordinal)) return null;
            return Seguridad.Verificar(contrasena, (string)fila["PasswordHash"], (string)fila["PasswordSalt"]) ? fila : null;
        }

        public static DataTable Listar(string filtro)
        {
            filtro = (filtro ?? "").Trim();
            return Bd.Consulta(
                "SELECT IdUsuario, Nombre, NombreCompleto, FechaRegistro FROM Usuario " +
                "WHERE Nombre LIKE @f OR NombreCompleto LIKE @f OR CAST(IdUsuario AS CHAR) = @i ORDER BY IdUsuario",
                Bd.P("@f", Bd.Contiene(filtro)), Bd.P("@i", filtro));
        }

        public static string Insertar(string nombre, string nombreCompleto, string contrasena)
        {
            Seguridad.CrearHash(contrasena, out string hash, out string sal);
            return Bd.Escribir(
                "INSERT INTO Usuario (Nombre, NombreCompleto, PasswordHash, PasswordSalt, FechaRegistro) VALUES (@n, @c, @h, @s, @f)",
                Duplicado, Relacion, false,
                Bd.P("@n", nombre), Bd.P("@c", nombreCompleto), Bd.P("@h", hash), Bd.P("@s", sal), Bd.P("@f", DateTime.Now));
        }

        // Si la contrasena viene vacia se conserva la actual
        public static string Actualizar(int id, string nombre, string nombreCompleto, string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena))
                return Bd.Escribir("UPDATE Usuario SET Nombre = @n, NombreCompleto = @c WHERE IdUsuario = @id",
                    Duplicado, Relacion, true, Bd.P("@n", nombre), Bd.P("@c", nombreCompleto), Bd.P("@id", id));

            Seguridad.CrearHash(contrasena, out string hash, out string sal);
            return Bd.Escribir(
                "UPDATE Usuario SET Nombre = @n, NombreCompleto = @c, PasswordHash = @h, PasswordSalt = @s WHERE IdUsuario = @id",
                Duplicado, Relacion, true,
                Bd.P("@n", nombre), Bd.P("@c", nombreCompleto), Bd.P("@h", hash), Bd.P("@s", sal), Bd.P("@id", id));
        }

        public static string Eliminar(int id)
        {
            if (id == Sesion.IdUsuario) return "No puedes eliminar el usuario con el que iniciaste sesión.";
            if (Convert.ToInt64(Bd.Escalar("SELECT COUNT(*) FROM Usuario")) <= 1) return "Debe quedar al menos un usuario en el sistema.";
            return Bd.Escribir("DELETE FROM Usuario WHERE IdUsuario = @id", Duplicado, Relacion, true, Bd.P("@id", id));
        }
    }
}
