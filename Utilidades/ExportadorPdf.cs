using System;
using System.Data;
using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace Proyecto_facturas.Utilidades
{
    // Reporte de ventas en PDF (MigraDoc). Usa las mismas columnas que ExportadorExcel.
    internal static class ExportadorPdf
    {
        private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

        public static void Exportar(DataTable ventas, string ruta, string titulo, string subtitulo)
        {
            Color azul = Color.FromRgb(Estilo.Primario.R, Estilo.Primario.G, Estilo.Primario.B);

            Document doc = new Document();
            doc.Info.Title = titulo;
            Style normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            Section seccion = doc.AddSection();
            seccion.PageSetup.PageFormat = PageFormat.A4;
            seccion.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            seccion.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
            seccion.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
            seccion.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);

            Paragraph t = seccion.AddParagraph(titulo);
            t.Format.Font.Size = 18;
            t.Format.Font.Bold = true;
            t.Format.Font.Color = azul;
            Paragraph s = seccion.AddParagraph(subtitulo);
            s.Format.Font.Color = Colors.Gray;
            s.Format.SpaceAfter = Unit.FromCentimeter(0.5);

            Table tabla = seccion.AddTable();
            tabla.Borders.Color = Color.FromRgb(209, 213, 219);
            tabla.Borders.Width = 0.5;
            tabla.Format.SpaceBefore = 2;
            tabla.Format.SpaceAfter = 2;

            string[] encabezados = { "N.º", "Cliente", "Fecha", "Artíc.", "Subtotal", "IVA", "Total" };
            double[] anchos = { 1.6, 5.2, 3.0, 1.6, 2.2, 2.0, 2.4 };
            ParagraphAlignment[] alineacion =
            {
                ParagraphAlignment.Left, ParagraphAlignment.Left, ParagraphAlignment.Left,
                ParagraphAlignment.Right, ParagraphAlignment.Right, ParagraphAlignment.Right, ParagraphAlignment.Right
            };

            for (int i = 0; i < anchos.Length; i++)
            {
                Column c = tabla.AddColumn(Unit.FromCentimeter(anchos[i]));
                c.Format.Alignment = alineacion[i];
            }

            Row cabecera = tabla.AddRow();
            cabecera.HeadingFormat = true;
            cabecera.Shading.Color = azul;
            cabecera.Format.Font.Color = Colors.White;
            cabecera.Format.Font.Bold = true;
            cabecera.VerticalAlignment = VerticalAlignment.Center;
            for (int i = 0; i < encabezados.Length; i++)
                cabecera.Cells[i].AddParagraph(encabezados[i]);

            for (int f = 0; f < ventas.Rows.Count; f++)
            {
                DataRow r = ventas.Rows[f];
                Row fila = tabla.AddRow();
                if (f % 2 == 1) fila.Shading.Color = Color.FromRgb(249, 250, 251);

                fila.Cells[0].AddParagraph(Convert.ToString(r["Idfactura"], Cultura));
                fila.Cells[1].AddParagraph(Convert.ToString(r["Cliente"]));
                fila.Cells[2].AddParagraph(Convert.ToDateTime(r["Fecha"]).ToString("dd/MM/yyyy HH:mm", Cultura));
                fila.Cells[3].AddParagraph(Convert.ToString(r["Articulos"], Cultura));
                fila.Cells[4].AddParagraph(Dinero(Convert.ToDouble(r["Subtotal"])));
                fila.Cells[5].AddParagraph(Dinero(Convert.ToDouble(r["IVA"])));
                fila.Cells[6].AddParagraph(Dinero(Convert.ToDouble(r["Total"])));
            }

            Row total = tabla.AddRow();
            total.Format.Font.Bold = true;
            total.Shading.Color = Color.FromRgb(219, 234, 254);
            total.Cells[0].MergeRight = 2;
            total.Cells[0].AddParagraph("Total (" + ventas.Rows.Count + " facturas)");
            total.Cells[3].AddParagraph(ExportadorExcel.Suma(ventas, "Articulos").ToString("N0", Cultura));
            total.Cells[4].AddParagraph(Dinero(ExportadorExcel.Suma(ventas, "Subtotal")));
            total.Cells[5].AddParagraph(Dinero(ExportadorExcel.Suma(ventas, "IVA")));
            total.Cells[6].AddParagraph(Dinero(ExportadorExcel.Suma(ventas, "Total")));

            Paragraph pie = seccion.Footers.Primary.AddParagraph();
            pie.Format.Alignment = ParagraphAlignment.Center;
            pie.Format.Font.Size = 8;
            pie.Format.Font.Color = Colors.Gray;
            pie.AddText("Página ");
            pie.AddPageField();
            pie.AddText(" de ");
            pie.AddNumPagesField();

            PdfDocumentRenderer render = new PdfDocumentRenderer(true) { Document = doc };
            render.RenderDocument();
            render.PdfDocument.Save(ruta);
        }

        private static string Dinero(double valor)
        {
            return valor.ToString("$#,##0.00", Cultura);
        }
    }
}
