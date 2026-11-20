using System.Drawing;
using System.Drawing.Text;
using System.Linq;

namespace Proyecto_facturas.Utilidades
{
    // Iconos de linea de la fuente de Windows (Segoe Fluent Icons en Windows 11, Segoe MDL2 Assets en Windows 10).
    // Si la fuente no existe simplemente no se ven; la aplicacion sigue siendo usable.
    internal static class Iconos
    {
        public const string Usuario = "", Candado = "", Ojo = "", Buscar = "",
            Agregar = "", Editar = "", Eliminar = "", Guardar = "", Cerrar = "",
            Check = "", Error = "", Alerta = "", Info = "", Clientes = "",
            Productos = "", Factura = "", Reporte = "", Salir = "", Calendario = "",
            Correo = "", Telefono = "", Ubicacion = "", Descargar = "", Inicio = "",
            Limpiar = "", Actualizar = "", Menos = "", Etiqueta = "";

        private static string familia;

        public static string Familia
        {
            get
            {
                if (familia != null) return familia;
                using (InstalledFontCollection fuentes = new InstalledFontCollection())
                {
                    bool fluent = fuentes.Families.Any(f => f.Name == "Segoe Fluent Icons");
                    familia = fluent ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
                }
                return familia;
            }
        }

        // Fuente de iconos con tamano en pixeles
        public static Font FuentePx(float px)
        {
            return new Font(Familia, px, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        // Dibuja un icono centrado en el area indicada
        public static void Dibujar(Graphics g, string glifo, float px, Color color, RectangleF area)
        {
            using (Font f = FuentePx(px))
            using (SolidBrush b = new SolidBrush(color))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                TextRenderingHint anterior = g.TextRenderingHint;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString(glifo, f, b, area, sf);
                g.TextRenderingHint = anterior;
            }
        }

        // Imagen cuadrada con el icono (para menus y botones)
        public static Bitmap Imagen(string glifo, int tam, Color color)
        {
            Bitmap bmp = new Bitmap(tam, tam);
            using (Graphics g = Graphics.FromImage(bmp))
                Dibujar(g, glifo, tam * 0.8f, color, new RectangleF(0, 0, tam, tam));
            return bmp;
        }
    }
}
