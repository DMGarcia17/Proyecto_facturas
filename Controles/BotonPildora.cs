using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    internal enum Variante { Primario, Exito, Peligro, Secundario }

    // Boton tipo pildora con estados normal / hover / presionado / deshabilitado e icono opcional
    internal class BotonPildora : Button
    {
        private bool hover, presionado;
        private Variante variante = Variante.Primario;
        private string icono = "";

        public BotonPildora()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Estilo.Boton;
            Size = new Size(140, 40);
            UseVisualStyleBackColor = false;
        }

        public Variante Variante { get { return variante; } set { variante = value; Invalidate(); } }
        public string Icono { get { return icono; } set { icono = value ?? ""; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; presionado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { presionado = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { presionado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { hover = presionado = false; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Estilo.Fondo);

            Color relleno, texto, borde = Color.Transparent;
            bool lleno = variante != Variante.Secundario;
            if (!Enabled)
            {
                relleno = lleno ? Color.FromArgb(238, 230, 238) : Color.White;
                texto = Color.FromArgb(176, 166, 176);
                borde = lleno ? Color.Transparent : Estilo.Borde;
            }
            else
            {
                Color baseColor, hoverColor;
                switch (variante)
                {
                    case Variante.Exito: baseColor = Estilo.ExitoBoton; hoverColor = Estilo.ExitoOscuro; break;
                    case Variante.Peligro: baseColor = Estilo.ErrorBoton; hoverColor = Color.FromArgb(160, 72, 62); break;
                    case Variante.Secundario: baseColor = Color.White; hoverColor = Estilo.PrimarioClaro; break;
                    default: baseColor = Estilo.Primario; hoverColor = Estilo.PrimarioOscuro; break;
                }
                relleno = presionado || hover ? hoverColor : baseColor;
                texto = lleno ? Color.White : Estilo.Texto;
                if (!lleno) borde = hover ? Estilo.Foco : Estilo.BordeCampo;
            }

            RectangleF area = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (GraphicsPath ruta = Estilo.Redondeado(area, area.Height / 2))
            {
                using (SolidBrush b = new SolidBrush(relleno)) g.FillPath(b, ruta);
                if (borde != Color.Transparent)
                    using (Pen p = new Pen(borde)) g.DrawPath(p, ruta);
                if (Focused && ShowFocusCues)
                    using (GraphicsPath anillo = Estilo.Redondeado(new RectangleF(3, 3, Width - 7, Height - 7), (Height - 7) / 2f))
                    using (Pen p = new Pen(Color.FromArgb(150, lleno ? Color.White : Estilo.Foco), 1.5f))
                        g.DrawPath(p, anillo);
            }

            // Icono + texto centrados
            Size medida = TextRenderer.MeasureText(g, Text, Font, new Size(Width, Height), TextFormatFlags.NoPadding);
            int anchoIcono = icono.Length > 0 ? 20 : 0;
            int separacion = icono.Length > 0 && Text.Length > 0 ? 8 : 0;
            int x = (Width - (anchoIcono + separacion + medida.Width)) / 2;
            if (icono.Length > 0)
            {
                Iconos.Dibujar(g, icono, 15, texto, new RectangleF(x, 0, anchoIcono, Height));
                x += anchoIcono + separacion;
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, medida.Width + 4, Height), texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.Left);
        }
    }
}
