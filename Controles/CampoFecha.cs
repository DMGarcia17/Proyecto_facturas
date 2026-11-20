using System;
using System.Drawing;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Selector de fecha con el mismo marco, etiqueta y mensaje de error que los demas campos.
    // El DateTimePicker dibuja su propio borde; se coloca dentro de un recorte para que solo se vea el del marco.
    internal class CampoFecha : CampoBase
    {
        private readonly Panel recorte = new Panel { BackColor = Color.White };
        private readonly DateTimePicker selector = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10.5F), CalendarForeColor = Estilo.Texto
        };

        public CampoFecha()
        {
            recorte.Controls.Add(selector);
            Montar(recorte);
            marco.Resize += (s, e) => Reubicar();
            selector.ValueChanged += (s, e) => OnValorCambiado();
            // Enfocar el marco equivale a enfocar el selector
            selector.Enter += (s, e) => marco.Foco = true;
            selector.Leave += (s, e) => marco.Foco = false;
            Reubicar();
        }

        // El selector (no el recorte) es el control que recibe el foco y dispara Validating
        public override Control Interior { get { return selector; } }
        public DateTimePicker Selector { get { return selector; } }
        public DateTime Value { get { return selector.Value; } set { selector.Value = value; } }

        protected override void AlCambiarHabilitado()
        {
            recorte.BackColor = Enabled ? Color.White : Estilo.CampoDeshabilitado;
        }

        private void Reubicar()
        {
            int ancho = Math.Max(20, marco.Width - 26);
            int alto = selector.Height - 6;
            recorte.SetBounds(12, (marco.Height - alto) / 2, ancho, alto);
            selector.SetBounds(-2, -3, ancho + 4, selector.Height);
        }
    }
}
