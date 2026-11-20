using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Cuadros de dialogo con el estilo de la aplicacion (confirmaciones y errores graves)
    internal static class Dialogo
    {
        public static bool Confirmar(IWin32Window dueno, string titulo, string mensaje, string si, string no, bool peligro)
        {
            using (FrmDialogo d = new FrmDialogo(peligro ? TipoAviso.Error : TipoAviso.Info, titulo, mensaje, si, no, peligro))
                return d.ShowDialog(dueno) == DialogResult.Yes;
        }

        public static void Error(IWin32Window dueno, string titulo, string mensaje)
        {
            using (FrmDialogo d = new FrmDialogo(TipoAviso.Error, titulo, mensaje, "Entendido", null, false))
                d.ShowDialog(dueno);
        }

        private class FrmDialogo : Form
        {
            private readonly TipoAviso tipo;
            private const int Ancho = 460, XTexto = 98;

            public FrmDialogo(TipoAviso tipo, string titulo, string mensaje, string si, string no, bool peligro)
            {
                this.tipo = tipo;
                Estilo.Formulario(this);
                BackColor = Color.White;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterParent;
                ShowInTaskbar = false;
                DoubleBuffered = true;

                int anchoTexto = Ancho - XTexto - 28;
                Size medidaTitulo = TextRenderer.MeasureText(titulo, Estilo.Subtitulo, new Size(anchoTexto, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                Size medidaMensaje = TextRenderer.MeasureText(mensaje, Estilo.Normal, new Size(anchoTexto, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                int contenido = Math.Max(56, medidaTitulo.Height + 8 + medidaMensaje.Height);
                ClientSize = new Size(Ancho, 28 + contenido + 28 + 40 + 26);

                Controls.Add(new Label
                {
                    Text = titulo, Font = Estilo.Subtitulo, ForeColor = Estilo.Texto, BackColor = Color.White,
                    Location = new Point(XTexto, 28), Size = new Size(anchoTexto, medidaTitulo.Height + 2)
                });
                Controls.Add(new Label
                {
                    Text = mensaje, Font = Estilo.Normal, ForeColor = Estilo.Gris, BackColor = Color.White,
                    Location = new Point(XTexto, 28 + medidaTitulo.Height + 8), Size = new Size(anchoTexto, medidaMensaje.Height + 4)
                });

                int y = ClientSize.Height - 26 - 40;
                BotonPildora btnSi = new BotonPildora
                {
                    Text = si, Width = Math.Max(120, TextRenderer.MeasureText(si, Estilo.Boton).Width + 44), Location = new Point(0, y),
                    Variante = peligro ? Variante.Peligro : Variante.Primario, DialogResult = DialogResult.Yes
                };
                btnSi.Left = ClientSize.Width - 28 - btnSi.Width;
                Controls.Add(btnSi);

                if (no != null)
                {
                    BotonPildora btnNo = new BotonPildora
                    {
                        Text = no, Width = 120, Location = new Point(btnSi.Left - 12 - 120, y),
                        Variante = Variante.Secundario, DialogResult = DialogResult.No
                    };
                    Controls.Add(btnNo);
                    CancelButton = btnNo;
                    // En acciones peligrosas, Enter equivale a cancelar (mas seguro)
                    AcceptButton = peligro ? btnNo : btnSi;
                    ActiveControl = peligro ? btnNo : btnSi;
                }
                else
                {
                    AcceptButton = btnSi;
                    CancelButton = btnSi;
                    ActiveControl = btnSi;
                }

                Region = new Region(Estilo.Redondeado(new Rectangle(0, 0, Width, Height), 16));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color tinte, fuerte; string glifo;
                switch (tipo)
                {
                    case TipoAviso.Error: tinte = Estilo.ErrorClaro; fuerte = Estilo.ErrorTexto; glifo = Iconos.Alerta; break;
                    case TipoAviso.Exito: tinte = Estilo.ExitoClaro; fuerte = Estilo.ExitoOscuro; glifo = Iconos.Check; break;
                    case TipoAviso.Aviso: tinte = Estilo.AvisoClaro; fuerte = Estilo.AvisoTexto; glifo = Iconos.Alerta; break;
                    default: tinte = Estilo.PrimarioClaro; fuerte = Estilo.Primario; glifo = Iconos.Info; break;
                }
                using (SolidBrush b = new SolidBrush(tinte)) g.FillEllipse(b, 28, 28, 56, 56);
                Iconos.Dibujar(g, glifo, 26, fuerte, new RectangleF(28, 28, 56, 56));
                using (Pen p = new Pen(Color.FromArgb(215, 200, 215), 2))
                    g.DrawPath(p, Estilo.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), 16));
            }
        }
    }
}
