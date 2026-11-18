using System;
using System.Drawing;
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
            ClientSize = new Size(440, 150);
            BackColor = Color.White;
            ShowInTaskbar = false;

            Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Estilo.Primario });
            Controls.Add(new Label
            {
                AutoSize = true, Text = "Sistema de facturación", Font = new Font("Segoe UI Semibold", 15F),
                Location = new Point(24, 30)
            });
            Controls.Add(new Label
            {
                AutoSize = true, Text = "Preparando la base de datos…", ForeColor = Estilo.Gris, Location = new Point(26, 72)
            });
            Controls.Add(new ProgressBar
            {
                Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Location = new Point(26, 106), Size = new Size(388, 10)
            });
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
