using System;
using System.Drawing;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Lista desplegable con el mismo marco, etiqueta y mensaje de error que los campos de texto
    internal class CampoLista : CampoBase
    {
        private readonly ComboBox combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10.5F),
            BackColor = Color.White, ForeColor = Estilo.Texto, DropDownWidth = 420
        };

        public CampoLista()
        {
            Montar(combo);
            marco.Resize += (s, e) => combo.SetBounds(10, (marco.Height - combo.Height) / 2, Math.Max(10, marco.Width - 20), combo.Height);
            combo.SelectedIndexChanged += (s, e) => OnValorCambiado();
            combo.SetBounds(10, 5, 100, combo.Height);
        }

        public override Control Interior { get { return combo; } }
        public ComboBox Combo { get { return combo; } }

        public object DataSource { get { return combo.DataSource; } set { combo.DataSource = value; } }
        public string DisplayMember { get { return combo.DisplayMember; } set { combo.DisplayMember = value; } }
        public string ValueMember { get { return combo.ValueMember; } set { combo.ValueMember = value; } }
        public int SelectedIndex { get { return combo.SelectedIndex; } set { combo.SelectedIndex = value; } }
        public object SelectedItem { get { return combo.SelectedItem; } }
        public object SelectedValue { get { return combo.SelectedValue; } }
        public override string Text { get { return combo.Text; } set { combo.Text = value; } }

        protected override void AlCambiarHabilitado()
        {
            combo.BackColor = Enabled ? Color.White : Estilo.CampoDeshabilitado;
        }
    }
}
