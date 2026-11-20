using System.Windows.Forms;

namespace Proyecto_facturas.Controles
{
    // Apila controles verticalmente con un TableLayoutPanel (el orden es explicito, sin depender del orden de Dock)
    internal static class Disposicion
    {
        // "llena" es el indice del control que ocupa el espacio sobrante (-1 = ninguno)
        public static TableLayoutPanel Pila(int llena, params Control[] hijos)
        {
            TableLayoutPanel t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = hijos.Length, Margin = Padding.Empty, Padding = Padding.Empty
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < hijos.Length; i++)
            {
                t.RowStyles.Add(i == llena ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
                hijos[i].Dock = DockStyle.Fill;
                t.Controls.Add(hijos[i], 0, i);
            }
            return t;
        }

        // Coloca un boton a la altura de la caja de un campo (debajo de su etiqueta) para alinearlo en una fila de campos
        public static Panel AlineadoACampo(Control boton)
        {
            // El panel nace con el ancho del boton para que el Anchor no lo encoja al acoplarse a su celda
            Panel p = new Panel { Width = boton.Width, Height = CampoBase.AltoCompleto, Margin = Padding.Empty };
            boton.Location = new System.Drawing.Point(0, 24);
            boton.Size = new System.Drawing.Size(boton.Width, CampoBase.AltoCaja);
            p.Controls.Add(boton);
            boton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return p;
        }

        // Fila de columnas: cada peso es un porcentaje relativo; un peso negativo es un ancho fijo en pixeles
        public static TableLayoutPanel Fila(float[] pesos, params Control[] hijos)
        {
            // Sin AutoSize: el ancho lo da la fila contenedora (con AutoSize las columnas porcentuales se encogen)
            // y el alto es el del control mas alto.
            int alto = 0;
            foreach (Control h in hijos) alto = System.Math.Max(alto, h.Height + h.Margin.Vertical);
            TableLayoutPanel t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = hijos.Length, RowCount = 1, Height = alto,
                Margin = Padding.Empty, Padding = Padding.Empty
            };
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < hijos.Length; i++)
            {
                t.ColumnStyles.Add(pesos[i] < 0 ? new ColumnStyle(SizeType.Absolute, -pesos[i]) : new ColumnStyle(SizeType.Percent, pesos[i]));
                hijos[i].Dock = DockStyle.Fill;
                hijos[i].Margin = new Padding(0, 0, i < hijos.Length - 1 ? 16 : 0, 0);
                t.Controls.Add(hijos[i], i, 0);
            }
            return t;
        }
    }
}
