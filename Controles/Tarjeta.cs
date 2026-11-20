using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Tarjeta blanca de esquinas redondeadas con sombra suave y titulo opcional (con icono o numero de paso).
    // El control incluye un margen de 8 px para dibujar la sombra, por eso el contenido va con Padding.
    internal class Tarjeta : Panel
    {
        public const int Sombra = 8;
        private string titulo = "", icono = "";
        private int numero;
        private Padding interior = new Padding(20, 16, 20, 16);

        public Tarjeta()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
            Margin = Padding.Empty;
            AplicarPadding();
        }

        public string Titulo { get { return titulo; } set { titulo = value ?? ""; AplicarPadding(); Invalidate(); } }
        public string Icono { get { return icono; } set { icono = value ?? ""; Invalidate(); } }
        public int Numero { get { return numero; } set { numero = value; Invalidate(); } }

        // Espacio entre el borde de la tarjeta y su contenido (sin contar la sombra ni el titulo)
        public Padding Interior { get { return interior; } set { interior = value; AplicarPadding(); } }

        private int AltoTitulo { get { return titulo.Length > 0 ? 44 : 0; } }

        private void AplicarPadding()
        {
            Padding = new Padding(Sombra + interior.Left, Sombra + interior.Top + AltoTitulo, Sombra + interior.Right, Sombra + interior.Bottom);
        }

        protected override void OnParentChanged(System.EventArgs e)
        {
            base.OnParentChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush fondo = new SolidBrush(Parent != null ? Parent.BackColor : Estilo.Fondo))
                g.FillRectangle(fondo, ClientRectangle);

            Rectangle cuerpo = new Rectangle(Sombra, Sombra - 2, Width - 2 * Sombra - 1, Height - 2 * Sombra - 1);
            if (cuerpo.Width < 10 || cuerpo.Height < 10) return;

            // Sombra suave: capas semitransparentes cada vez mas grandes
            for (int i = 6; i >= 1; i--)
            {
                Rectangle r = new Rectangle(cuerpo.X - i, cuerpo.Y - i + 3, cuerpo.Width + 2 * i, cuerpo.Height + 2 * i);
                using (GraphicsPath p = Estilo.Redondeado(r, 14 + i))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(7, 96, 64, 96)))
                    g.FillPath(b, p);
            }

            using (GraphicsPath cuerpoRuta = Estilo.Redondeado(cuerpo, 14))
            {
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, cuerpoRuta);
                using (Pen lapiz = new Pen(Estilo.Borde)) g.DrawPath(lapiz, cuerpoRuta);
            }

            if (titulo.Length == 0) return;

            int x = Sombra + interior.Left, y = Sombra + interior.Top;
            if (numero > 0)
            {
                using (SolidBrush b = new SolidBrush(Estilo.PrimarioClaro)) g.FillEllipse(b, x, y, 28, 28);
                TextRenderer.DrawText(g, numero.ToString(), Estilo.Etiqueta, new Rectangle(x, y, 28, 28), Estilo.Primario,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += 38;
            }
            else if (icono.Length > 0)
            {
                Iconos.Dibujar(g, icono, 18, Estilo.Primario, new RectangleF(x, y, 28, 28));
                x += 36;
            }
            TextRenderer.DrawText(g, titulo, Estilo.Subtitulo, new Point(x, y + 3), Estilo.Texto, TextFormatFlags.NoPadding);
        }
    }
}
