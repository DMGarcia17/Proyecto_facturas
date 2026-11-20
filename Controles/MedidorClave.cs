using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Medidor de fortaleza de contrasena con lista de requisitos que se marcan en vivo
    internal class MedidorClave : Control
    {
        private string clave = "";
        private bool vacia = true;

        public MedidorClave()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Height = 62;
            Margin = Padding.Empty;
        }

        public override Size GetPreferredSize(Size proposedSize) { return new Size(proposedSize.Width > 0 ? proposedSize.Width : 300, 62); }

        public void Evaluar(string texto)
        {
            clave = texto ?? "";
            vacia = clave.Length == 0;
            Invalidate();
        }

        private bool Largo { get { return clave.Length >= 8; } }
        private bool Letra { get { return clave.Any(char.IsLetter); } }
        private bool Numero { get { return clave.Any(char.IsDigit); } }

        // 0 = sin escribir, 1 = debil, 2 = media, 3 = fuerte
        private int Nivel
        {
            get
            {
                if (vacia) return 0;
                int puntos = (Largo ? 1 : 0) + (Letra ? 1 : 0) + (Numero ? 1 : 0);
                if (puntos < 3) return 1;
                bool mixta = clave.Any(char.IsUpper) && clave.Any(char.IsLower);
                bool especial = clave.Any(c => !char.IsLetterOrDigit(c));
                return clave.Length >= 12 && (mixta || especial) ? 3 : 2;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Color.White);

            int nivel = Nivel;
            Color[] colores = { Estilo.ErrorBorde, Estilo.AvisoTexto, Estilo.ExitoBoton };
            string[] nombres = { "Débil", "Media", "Fuerte" };

            int ancho = (Width - 12 - 90) / 3;
            for (int i = 0; i < 3; i++)
            {
                Color c = i < nivel ? colores[nivel - 1] : Color.FromArgb(235, 227, 235);
                using (GraphicsPath p = Estilo.Redondeado(new RectangleF(i * (ancho + 6), 4, ancho, 8), 4))
                using (SolidBrush b = new SolidBrush(c))
                    g.FillPath(b, p);
            }
            string etiqueta = nivel == 0 ? "Seguridad" : nombres[nivel - 1];
            TextRenderer.DrawText(g, etiqueta, Estilo.Pequena, new Point(3 * (ancho + 6) + 4, 0),
                nivel == 0 ? Estilo.Gris : colores[nivel - 1], TextFormatFlags.NoPadding);

            DibujarRequisito(g, "8 o más caracteres", Largo, 0, 24);
            DibujarRequisito(g, "Una letra", Letra, 1, 24);
            DibujarRequisito(g, "Un número", Numero, 2, 24);
        }

        private void DibujarRequisito(Graphics g, string texto, bool cumple, int columna, int y)
        {
            int x = columna * (Width / 3);
            Color color = vacia ? Estilo.Gris : cumple ? Estilo.ExitoOscuro : Estilo.Gris;
            Iconos.Dibujar(g, cumple ? Iconos.Check : Iconos.Menos, 13, cumple ? Estilo.ExitoOscuro : Estilo.BordeCampo, new RectangleF(x, y, 18, 20));
            TextRenderer.DrawText(g, texto, Estilo.Pequena, new Point(x + 20, y + 2), color, TextFormatFlags.NoPadding);
        }
    }
}
