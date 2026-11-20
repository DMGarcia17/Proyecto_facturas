using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Controles
{
    // Bloque de un formulario: titulo y descripcion de la seccion, seguido de filas de campos en columnas
    internal class SeccionForm : TableLayoutPanel
    {
        public SeccionForm(string titulo, string descripcion)
        {
            AutoSize = true;
            ColumnCount = 1;
            Margin = new Padding(0, 0, 0, 14);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            BackColor = Color.White;

            Agregar(new Label
            {
                Text = titulo, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Estilo.Primario, AutoSize = true,
                Margin = new Padding(0, 0, 0, 0)
            });
            if (!string.IsNullOrEmpty(descripcion))
                Agregar(new Label
                {
                    Text = descripcion, Font = Estilo.Pequena, ForeColor = Estilo.Gris, AutoSize = true, Margin = new Padding(0, 1, 0, 10)
                });
            else
                Padding = new Padding(0, 0, 0, 0);
        }

        // Fila con columnas del mismo ancho
        public SeccionForm Fila(params Control[] campos)
        {
            return Fila(campos.Select(c => 1f).ToArray(), campos);
        }

        // Fila con anchos relativos (por ejemplo {2, 1} = 2/3 y 1/3)
        public SeccionForm Fila(float[] pesos, params Control[] campos)
        {
            foreach (Control c in campos) c.Dock = DockStyle.Fill;
            Agregar(Disposicion.Fila(pesos, campos));
            return this;
        }

        private void Agregar(Control c)
        {
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (c is TableLayoutPanel) c.Dock = DockStyle.Fill;
            Controls.Add(c, 0, RowCount++);
        }
    }
}
