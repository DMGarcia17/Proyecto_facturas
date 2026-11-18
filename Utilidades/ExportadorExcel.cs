using System;
using System.Data;
using ClosedXML.Excel;

namespace Proyecto_facturas.Utilidades
{
    // Reporte de ventas en .xlsx. Espera las columnas de FacturaDatos.Listar:
    // Idfactura, Cliente, Fecha, Articulos, Subtotal, IVA, Total
    internal static class ExportadorExcel
    {
        public static void Exportar(DataTable ventas, string ruta, string titulo, string subtitulo)
        {
            using (XLWorkbook libro = new XLWorkbook())
            {
                IXLWorksheet hoja = libro.Worksheets.Add("Ventas");
                string[] encabezados = { "N.º factura", "Cliente", "Fecha", "Artículos", "Subtotal", "IVA", "Total" };
                int columnas = encabezados.Length;

                hoja.Cell(1, 1).Value = titulo;
                hoja.Cell(1, 1).Style.Font.Bold = true;
                hoja.Cell(1, 1).Style.Font.FontSize = 16;
                hoja.Cell(2, 1).Value = subtitulo;
                hoja.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

                const int filaEncabezado = 4;
                for (int c = 0; c < columnas; c++)
                    hoja.Cell(filaEncabezado, c + 1).Value = encabezados[c];

                IXLRange encabezado = hoja.Range(filaEncabezado, 1, filaEncabezado, columnas);
                encabezado.Style.Font.Bold = true;
                encabezado.Style.Font.FontColor = XLColor.White;
                encabezado.Style.Fill.BackgroundColor = XLColor.FromArgb(Estilo.Primario.R, Estilo.Primario.G, Estilo.Primario.B);

                for (int f = 0; f < ventas.Rows.Count; f++)
                {
                    DataRow r = ventas.Rows[f];
                    int fila = filaEncabezado + 1 + f;
                    hoja.Cell(fila, 1).Value = Convert.ToInt64(r["Idfactura"]);
                    hoja.Cell(fila, 2).Value = Convert.ToString(r["Cliente"]);
                    hoja.Cell(fila, 3).Value = Convert.ToDateTime(r["Fecha"]);
                    hoja.Cell(fila, 3).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    hoja.Cell(fila, 4).Value = Convert.ToInt64(r["Articulos"]);
                    Dinero(hoja.Cell(fila, 5), Convert.ToDouble(r["Subtotal"]));
                    Dinero(hoja.Cell(fila, 6), Convert.ToDouble(r["IVA"]));
                    Dinero(hoja.Cell(fila, 7), Convert.ToDouble(r["Total"]));
                }

                // Fila de totales
                int filaTotal = filaEncabezado + ventas.Rows.Count + 1;
                hoja.Cell(filaTotal, 2).Value = "Total (" + ventas.Rows.Count + " facturas)";
                hoja.Cell(filaTotal, 4).Value = Suma(ventas, "Articulos");
                Dinero(hoja.Cell(filaTotal, 5), Suma(ventas, "Subtotal"));
                Dinero(hoja.Cell(filaTotal, 6), Suma(ventas, "IVA"));
                Dinero(hoja.Cell(filaTotal, 7), Suma(ventas, "Total"));
                IXLRange filaTotales = hoja.Range(filaTotal, 1, filaTotal, columnas);
                filaTotales.Style.Font.Bold = true;
                filaTotales.Style.Border.TopBorder = XLBorderStyleValues.Thin;

                hoja.Columns().AdjustToContents();
                libro.SaveAs(ruta);
            }
        }

        private static void Dinero(IXLCell celda, double valor)
        {
            celda.Value = valor;
            celda.Style.NumberFormat.Format = "$#,##0.00";
        }

        internal static double Suma(DataTable tabla, string columna)
        {
            double suma = 0;
            foreach (DataRow fila in tabla.Rows)
                if (fila[columna] != DBNull.Value) suma += Convert.ToDouble(fila[columna]);
            return suma;
        }
    }
}
