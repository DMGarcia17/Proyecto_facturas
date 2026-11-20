using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Campo de texto: etiqueta, caja redondeada, icono o prefijo opcional, texto de ayuda y error en linea
    internal class CampoTexto : CampoBase
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private readonly TextBox caja = new TextBox
        {
            BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), ForeColor = Estilo.Texto, BackColor = Color.White
        };
        // Las etiquetas son transparentes y no tocan el borde del marco para no taparlo
        private readonly Label lblIcono = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Estilo.Gris, Visible = false, BackColor = Color.Transparent };
        private readonly Label lblPrefijo = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Estilo.Gris, Font = new Font("Segoe UI Semibold", 10.5F), Visible = false, BackColor = Color.Transparent };
        private readonly Label lblOjo = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Estilo.Gris, Cursor = Cursors.Hand, Visible = false, BackColor = Color.Transparent };
        private string placeholder = "", icono = "", prefijo = "";
        // Se guardan aparte porque Control.Visible devuelve false mientras la ventana no se muestra
        private bool ojoVisible, esContrasena, hayIcono, hayPrefijo;

        public CampoTexto()
        {
            Montar(caja);
            lblIcono.Font = Iconos.FuentePx(16);
            lblOjo.Font = Iconos.FuentePx(16);
            lblOjo.Text = Iconos.Ojo;
            marco.Controls.AddRange(new Control[] { lblIcono, lblPrefijo, lblOjo });
            marco.Resize += (s, e) => ReubicarInterior();
            caja.TextChanged += (s, e) => OnValorCambiado();
            caja.HandleCreated += (s, e) => AplicarPlaceholder();
            lblOjo.Click += (s, e) => { caja.UseSystemPasswordChar = !caja.UseSystemPasswordChar; lblOjo.ForeColor = caja.UseSystemPasswordChar ? Estilo.Gris : Estilo.Primario; };
            ReubicarInterior();
        }

        public override Control Interior { get { return caja; } }
        public TextBox Caja { get { return caja; } }

        public override string Text { get { return caja.Text; } set { caja.Text = value; } }
        public int MaxLength { get { return caja.MaxLength; } set { caja.MaxLength = value; } }

        public string Placeholder { get { return placeholder; } set { placeholder = value ?? ""; AplicarPlaceholder(); } }

        // Icono de la fuente de iconos, a la izquierda de la caja
        public string Icono
        {
            get { return icono; }
            set { icono = value ?? ""; hayIcono = icono.Length > 0; lblIcono.Text = icono; lblIcono.Visible = hayIcono; ReubicarInterior(); }
        }

        // Texto fijo a la izquierda (por ejemplo "$")
        public string Prefijo
        {
            get { return prefijo; }
            set { prefijo = value ?? ""; hayPrefijo = prefijo.Length > 0; lblPrefijo.Text = prefijo; lblPrefijo.Visible = hayPrefijo; ReubicarInterior(); }
        }

        public bool EsContrasena
        {
            get { return esContrasena; }
            set { esContrasena = value; caja.UseSystemPasswordChar = value; ReubicarInterior(); }
        }

        // Muestra el ojo para ver/ocultar la contrasena dentro del campo
        public bool OjoVisible { get { return ojoVisible; } set { ojoVisible = value; lblOjo.Visible = value; ReubicarInterior(); } }

        // Muestra u oculta los caracteres sin el ojo (por ejemplo, con un interruptor externo)
        public void MostrarCaracteres(bool mostrar) { caja.UseSystemPasswordChar = esContrasena && !mostrar; }

        private void AplicarPlaceholder()
        {
            if (caja.IsHandleCreated) SendMessage(caja.Handle, EM_SETCUEBANNER, new IntPtr(1), placeholder);
        }

        protected override void AlCambiarHabilitado()
        {
            caja.BackColor = Enabled ? Color.White : Estilo.CampoDeshabilitado;
        }

        private void ReubicarInterior()
        {
            int alto = marco.Height;
            int x = 14, derecha = marco.Width - 14;
            if (hayIcono) { lblIcono.SetBounds(8, 3, 28, alto - 6); x = 38; }
            if (hayPrefijo) { lblPrefijo.SetBounds(x - 2, 3, 18, alto - 6); x += 16; }
            if (ojoVisible) { lblOjo.SetBounds(marco.Width - 38, 3, 30, alto - 6); derecha = marco.Width - 40; }
            caja.SetBounds(x, (alto - caja.Height) / 2, Math.Max(10, derecha - x), caja.Height);
        }
    }
}
