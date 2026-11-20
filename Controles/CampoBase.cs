using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Marco redondeado de un campo: cambia de color con el foco y cuando hay error
    internal class Marco : Panel
    {
        private bool foco, conError, deshabilitado;

        public Marco()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public bool Foco { get { return foco; } set { foco = value; Invalidate(); } }
        public bool ConError { get { return conError; } set { conError = value; Invalidate(); } }
        public bool Deshabilitado { get { return deshabilitado; } set { deshabilitado = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Estilo.Fondo);

            RectangleF r = new RectangleF(1, 1, Width - 3, Height - 3);
            using (GraphicsPath ruta = Estilo.Redondeado(r, 11))
            {
                using (SolidBrush b = new SolidBrush(deshabilitado ? Estilo.CampoDeshabilitado : Color.White))
                    g.FillPath(b, ruta);
                Color borde = conError ? Estilo.ErrorBorde : foco ? Estilo.Foco : Estilo.BordeCampo;
                using (Pen p = new Pen(borde, conError || foco ? 1.8f : 1f))
                    g.DrawPath(p, ruta);
            }
        }
    }

    // Base de los campos de formulario: etiqueta (con * si es obligatorio), caja redondeada,
    // y debajo un texto de ayuda que se reemplaza por el mensaje de error cuando la validacion falla.
    internal abstract class CampoBase : Panel
    {
        public const int AltoCompleto = 84, AltoCompacto = 42, AltoCaja = 40;

        protected readonly Label lblMensaje = new Label { AutoSize = false, Font = Estilo.Pequena, ForeColor = Estilo.Gris, AutoEllipsis = true };
        protected readonly Marco marco = new Marco();
        private string etiqueta = "", ayuda = "", error = "";
        private bool obligatorio, compacto;

        // Se dispara cuando cambia el valor del campo (texto, seleccion o fecha)
        public event EventHandler ValorCambiado;

        protected CampoBase()
        {
            DoubleBuffered = true;
            Height = AltoCompleto;
            Margin = Padding.Empty;
            Controls.AddRange(new Control[] { marco, lblMensaje });
            marco.Click += (s, e) => Interior.Focus();
            Reubicar();
        }

        // Control real donde el usuario escribe o elige
        public abstract Control Interior { get; }

        protected void Montar(Control interior)
        {
            marco.Controls.Add(interior);
            interior.Enter += (s, e) => marco.Foco = true;
            interior.Leave += (s, e) => marco.Foco = false;
        }

        protected void OnValorCambiado()
        {
            ValorCambiado?.Invoke(this, EventArgs.Empty);
        }

        public string Etiqueta { get { return etiqueta; } set { etiqueta = value ?? ""; Reubicar(); Invalidate(); } }
        public bool Obligatorio { get { return obligatorio; } set { obligatorio = value; Invalidate(); } }
        public string Ayuda { get { return ayuda; } set { ayuda = value ?? ""; ActualizarMensaje(); } }
        public string Error { get { return error; } }

        // Modo compacto: solo la caja, sin etiqueta ni mensaje (por ejemplo, un buscador)
        public bool Compacto
        {
            get { return compacto; }
            set { compacto = value; Height = value ? AltoCompacto : AltoCompleto; Reubicar(); }
        }

        public void MostrarError(string mensaje)
        {
            error = mensaje ?? "";
            marco.ConError = error.Length > 0;
            ActualizarMensaje();
        }

        private void ActualizarMensaje()
        {
            bool hayError = error.Length > 0;
            lblMensaje.ForeColor = hayError ? Estilo.ErrorTexto : Estilo.Gris;
            lblMensaje.Text = hayError ? error : ayuda;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            marco.Deshabilitado = !Enabled;
            AlCambiarHabilitado();
        }

        protected virtual void AlCambiarHabilitado() { }

        // La etiqueta (y el * de obligatorio) se pintan directamente: asi nada se superpone ni recorta el texto
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (compacto || etiqueta.Length == 0) return;

            Size medida = TextRenderer.MeasureText(e.Graphics, etiqueta, Estilo.Etiqueta, new Size(int.MaxValue, 24), TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, etiqueta, Estilo.Etiqueta, new Point(1, 2), Enabled ? Estilo.Texto : Estilo.Gris, TextFormatFlags.NoPadding);
            if (obligatorio)
                TextRenderer.DrawText(e.Graphics, "*", Estilo.Etiqueta, new Point(1 + medida.Width + 1, 2), Estilo.ErrorTexto, TextFormatFlags.NoPadding);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Reubicar();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(Math.Max(120, proposedSize.Width > 0 ? Math.Min(proposedSize.Width, 400) : Width), compacto ? AltoCompacto : AltoCompleto);
        }

        private void Reubicar()
        {
            int ancho = Math.Max(10, Width);
            int y = !compacto && etiqueta.Length > 0 ? 24 : 0;
            marco.SetBounds(0, y, ancho, AltoCaja);
            lblMensaje.Visible = !compacto;
            lblMensaje.SetBounds(3, y + AltoCaja + 3, Math.Max(10, ancho - 6), 18);
        }
    }
}
