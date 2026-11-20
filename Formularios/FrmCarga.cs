using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Ventana de espera mientras se prepara la base de datos (crea tablas y datos iniciales si faltan)
    internal class FrmCarga : Form
    {
        private readonly Action accion;

        // Error ocurrido al ejecutar la accion (null si salio bien)
        public Exception Error { get; private set; }

        public FrmCarga(Action accion)
        {
            this.accion = accion;
            Estilo.Formulario(this);
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(460, 190);
            BackColor = Color.White;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Region = new Region(Estilo.Redondeado(new Rectangle(0, 0, Width, Height), 18));

            Controls.Add(new Label
            {
                AutoSize = true, Text = "Sistema de facturación", Font = new Font("Segoe UI Semilight", 18F),
                ForeColor = Estilo.Texto, BackColor = Color.White, Location = new Point(94, 38)
            });
            Controls.Add(new Label
            {
                AutoSize = true, Text = "Preparando la base de datos…", ForeColor = Estilo.Gris, BackColor = Color.White, Location = new Point(96, 78)
            });
            Controls.Add(new ProgressBar
            {
                Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Location = new Point(40, 130), Size = new Size(380, 8)
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath chip = Estilo.Redondeado(new RectangleF(40, 34, 44, 44), 14))
            using (SolidBrush b = new SolidBrush(Estilo.PrimarioClaro))
                g.FillPath(b, chip);
            Iconos.Dibujar(g, Iconos.Factura, 20, Estilo.Primario, new RectangleF(40, 34, 44, 44));
            using (Pen p = new Pen(Color.FromArgb(215, 200, 215), 2))
                g.DrawPath(p, Estilo.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), 18));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Task.Run(accion).ContinueWith(t =>
            {
                Error = t.Exception?.GetBaseException();
                if (!IsDisposed) BeginInvoke(new Action(Close));
            });
        }
    }
}
