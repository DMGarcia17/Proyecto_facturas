using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Interruptor (toggle) como el de la imagen de referencia, con texto a la derecha
    internal class Interruptor : Control
    {
        private bool activo;
        public event EventHandler CheckedChanged;

        public Interruptor()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Size = new Size(200, 30);
            Cursor = Cursors.Hand;
            Font = Estilo.Normal;
            TabStop = true;
        }

        public bool Checked
        {
            get { return activo; }
            set
            {
                if (activo == value) return;
                activo = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; } base.OnKeyDown(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Estilo.Fondo);

            RectangleF pista = new RectangleF(1, (Height - 24) / 2f, 46, 24);
            using (GraphicsPath ruta = Estilo.Redondeado(pista, 12))
            {
                using (SolidBrush b = new SolidBrush(activo ? Estilo.Acento : Color.White)) g.FillPath(b, ruta);
                using (Pen p = new Pen(activo ? Estilo.Foco : Estilo.BordeCampo, 1.5f)) g.DrawPath(p, ruta);
            }

            float d = 18;
            float x = activo ? pista.Right - d - 4 : pista.X + 4;
            RectangleF bola = new RectangleF(x, pista.Y + (pista.Height - d) / 2, d, d);
            using (SolidBrush sombra = new SolidBrush(Color.FromArgb(30, 60, 40, 60)))
                g.FillEllipse(sombra, bola.X, bola.Y + 1.5f, d, d);
            using (SolidBrush b = new SolidBrush(activo ? Color.White : Estilo.Acento)) g.FillEllipse(b, bola);

            if (Focused && ShowFocusCues)
                using (GraphicsPath foco = Estilo.Redondeado(new RectangleF(pista.X - 2, pista.Y - 2, pista.Width + 4, pista.Height + 4), 14))
                using (Pen p = new Pen(Color.FromArgb(120, Estilo.Foco), 1.2f))
                    g.DrawPath(p, foco);

            TextRenderer.DrawText(g, Text, Font, new Rectangle(56, 0, Math.Max(10, Width - 56), Height), Estilo.Texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }
}
