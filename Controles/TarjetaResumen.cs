using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Tarjeta pastel con icono, cifra grande y titulo (como las tarjetas de color de la imagen de referencia)
    internal class TarjetaResumen : Control
    {
        private string titulo = "", valor = "-", icono = "";
        private Color tinte = Estilo.PrimarioClaro, fuerte = Estilo.Primario;

        public TarjetaResumen()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Height = 84;
            Margin = new Padding(0);
        }

        public string Titulo { get { return titulo; } set { titulo = value ?? ""; Invalidate(); } }
        public string Valor { get { return valor; } set { valor = value ?? ""; Invalidate(); } }
        public string Icono { get { return icono; } set { icono = value ?? ""; Invalidate(); } }

        // Color de fondo suave y color del icono/cifra
        public void Colores(Color fondo, Color acento)
        {
            tinte = fondo;
            fuerte = acento;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Estilo.Fondo);
            Dibujar(g, new Rectangle(0, 0, Width, Height), icono, titulo, valor, tinte, fuerte);
        }

        // Tambien se usa para pintar tarjetas donde no se pueden poner controles (area MDI)
        public static void Dibujar(Graphics g, Rectangle r, string icono, string titulo, string valor, Color tinte, Color fuerte)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath ruta = Estilo.Redondeado(new RectangleF(r.X, r.Y, r.Width - 1, r.Height - 1), 16))
            using (SolidBrush b = new SolidBrush(tinte))
                g.FillPath(b, ruta);

            RectangleF circulo = new RectangleF(r.X + 16, r.Y + (r.Height - 46) / 2f, 46, 46);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(200, Color.White))) g.FillEllipse(b, circulo);
            Iconos.Dibujar(g, icono, 20, fuerte, circulo);

            int x = r.X + 76;
            TextRenderer.DrawText(g, valor, Estilo.Cifra, new Rectangle(x, r.Y + 12, r.Width - 76 - 8, 34), Estilo.Texto,
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, titulo, Estilo.Pequena, new Rectangle(x, r.Y + 48, r.Width - 76 - 8, 22), fuerte,
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }
}
