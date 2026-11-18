namespace Proyecto_facturas
{
    // Usuario que inicio sesion en la aplicacion
    internal static class Sesion
    {
        public static int IdUsuario { get; private set; }
        public static string Nombre { get; private set; }
        public static string NombreCompleto { get; private set; }

        public static void Iniciar(int id, string nombre, string nombreCompleto)
        {
            IdUsuario = id;
            Nombre = nombre;
            NombreCompleto = nombreCompleto;
        }

        public static void Cerrar()
        {
            IdUsuario = 0;
            Nombre = null;
            NombreCompleto = null;
        }
    }
}
