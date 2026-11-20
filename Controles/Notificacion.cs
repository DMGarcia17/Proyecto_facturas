using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    internal enum TipoAviso { Exito, Info, Aviso, Error }

    // Notificacion flotante (toast) en la esquina inferior derecha de la ventana; se oculta sola.
    internal static class Notificacion
    {
        public static void Mostrar(IWin32Window dueno, TipoAviso tipo, string titulo, string mensaje, int milisegundos = 3500)
        {
            Form f = dueno as Form;
            if (f != null && f.IsMdiContainer && f.ActiveMdiChild != null) f = f.ActiveMdiChild;
            if (f == null || f.IsDisposed || !f.IsHandleCreated) return;

            Toast toast = new Toast(tipo, titulo, mensaje, milisegundos);
            int desplazamiento = 0;
            foreach (Control c in f.Controls)
                if (c is Toast) desplazamiento += c.Height + 10;

            toast.Location = new Point(f.ClientSize.Width - toast.Width - 24, f.ClientSize.Height - toast.Height - 24 - desplazamiento);
            toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            f.Controls.Add(toast);
            toast.BringToFront();
        }

        private class Toast : Control
        {
            private const int Ancho = 380, MargenTexto = 76;
            private readonly TipoAviso tipo;
            private readonly string titulo, mensaje;
            private readonly Timer reloj = new Timer();

            public Toast(TipoAviso tipo, string titulo, string mensaje, int milisegundos)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.tipo = tipo;
                this.titulo = titulo ?? "";
                this.mensaje = mensaje ?? "";
                Cursor = Cursors.Hand;

                Size texto = TextRenderer.MeasureText(this.mensaje, Estilo.Pequena, new Size(Ancho - MargenTexto - 20, 0),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                Size = new Size(Ancho, Math.Max(70, 16 + 22 + Math.Min(texto.Height, 60) + 18));
                Region = new Region(Estilo.Redondeado(new Rectangle(0, 0, Width, Height), 14));

                reloj.Interval = Math.Max(1500, milisegundos);
                reloj.Tick += (s, e) => Cerrar();
                reloj.Start();
            }

            private void Cerrar()
            {
                reloj.Stop();
                if (Parent != null) Parent.Controls.Remove(this);
                Dispose();
            }

            protected override void OnClick(EventArgs e) { base.OnClick(e); Cerrar(); }

            protected override void Dispose(bool disposing)
            {
                if (disposing) reloj.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                Color tinte, fuerte; string glifo;
                switch (tipo)
                {
                    case TipoAviso.Exito: tinte = Estilo.ExitoClaro; fuerte = Estilo.ExitoOscuro; glifo = Iconos.Check; break;
                    case TipoAviso.Aviso: tinte = Estilo.AvisoClaro; fuerte = Estilo.AvisoTexto; glifo = Iconos.Alerta; break;
                    case TipoAviso.Error: tinte = Estilo.ErrorClaro; fuerte = Estilo.ErrorTexto; glifo = Iconos.Error; break;
                    default: tinte = Estilo.InfoClaro; fuerte = Estilo.InfoTexto; glifo = Iconos.Info; break;
                }

                using (Pen p = new Pen(Color.FromArgb(225, 214, 225), 2)) g.DrawPath(p, Estilo.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), 14));
                using (SolidBrush b = new SolidBrush(tinte)) g.FillEllipse(b, 18, (Height - 42) / 2, 42, 42);
                Iconos.Dibujar(g, glifo, 20, fuerte, new RectangleF(18, (Height - 42) / 2, 42, 42));

                TextRenderer.DrawText(g, titulo, Estilo.Etiqueta, new Point(MargenTexto, 14), Estilo.Texto, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, mensaje, Estilo.Pequena, new Rectangle(MargenTexto, 36, Width - MargenTexto - 18, Height - 40),
                    Estilo.Gris, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
