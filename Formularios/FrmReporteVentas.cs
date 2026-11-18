using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Reporte de ventas por rango de fechas, con filtro por cliente, exportable a Excel y PDF
    internal class FrmReporteVentas : Form
    {
        private readonly Validador validador = new Validador();
        private readonly DateTimePicker dtpDesde, dtpHasta;
        private readonly ComboBox cboCliente;
        private readonly Label lblFacturas, lblSubtotal, lblIva, lblTotal;
        private readonly Button btnExcel, btnPdf;
        private readonly DataGridView dgv;
        private DataTable ventas;
        private string subtitulo = "";

        private const string TodosLosClientes = "(Todos los clientes)";

        public FrmReporteVentas()
        {
            Estilo.Hijo(this);
            Text = "Reporte de ventas";
            ClientSize = new Size(980, 680);
            MinimumSize = new Size(980, 680);
            Estilo.Encabezado(this, "Reporte de ventas", "Ventas por rango de fechas, con exportación a Excel y PDF");

            Estilo.NuevaEtiqueta(this, "Desde", 24, 106);
            dtpDesde = Estilo.NuevaFecha(this, 24, 130, 150);
            dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Estilo.NuevaEtiqueta(this, "Hasta", 194, 106);
            dtpHasta = Estilo.NuevaFecha(this, 194, 130, 150);
            dtpHasta.Value = DateTime.Today;
            Estilo.NuevaEtiqueta(this, "Cliente", 364, 106);
            cboCliente = Estilo.NuevaCombo(this, 364, 130, 330);
            Button btnGenerar = Estilo.NuevoBoton(this, "Generar", 714, 122, 130, true);
            AcceptButton = btnGenerar;

            validador.AgregarRango(dtpDesde, dtpHasta);

            lblFacturas = Estilo.TarjetaResumen(this, "Facturas", 24, 180, 220, Estilo.Primario);
            lblSubtotal = Estilo.TarjetaResumen(this, "Subtotal", 264, 180, 220, Estilo.Primario);
            lblIva = Estilo.TarjetaResumen(this, "IVA", 504, 180, 220, Estilo.Primario);
            lblTotal = Estilo.TarjetaResumen(this, "Total vendido", 744, 180, 212, Estilo.Primario);

            btnPdf = Estilo.NuevoBoton(this, "Exportar a PDF", 786, 264, 170, true);
            btnPdf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPdf.Enabled = false;
            btnExcel = Estilo.NuevoBoton(this, "Exportar a Excel", 606, 264, 170, true);
            btnExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExcel.Enabled = false;

            Label t = Estilo.NuevaEtiqueta(this, "Facturas del período", 24, 274);
            t.Font = Estilo.Subtitulo;
            dgv = Estilo.NuevaTabla(this, 24, 316, 932, 340);

            btnGenerar.Click += (s, e) => Generar();
            btnExcel.Click += (s, e) => Exportar("xlsx");
            btnPdf.Click += (s, e) => Exportar("pdf");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                DataTable clientes = ClienteDatos.Nombres();
                DataRow todos = clientes.NewRow();
                todos["Nombre"] = TodosLosClientes;
                clientes.Rows.InsertAt(todos, 0);
                cboCliente.DataSource = clientes;
                cboCliente.DisplayMember = "Nombre";
                cboCliente.SelectedIndex = 0;
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
                return;
            }
            Generar();
        }

        private void Generar()
        {
            if (!validador.ValidarTodo())
            {
                Estilo.Aviso(this, "Corrige el rango de fechas.", "Datos incompletos");
                return;
            }

            string cliente = cboCliente.SelectedIndex > 0 ? cboCliente.Text : "";
            try
            {
                ventas = FacturaDatos.Listar(dtpDesde.Value, dtpHasta.Value, "");
                if (cliente.Length > 0)
                {
                    // Filtro exacto por cliente sobre el resultado
                    ventas.DefaultView.RowFilter = "Cliente = '" + cliente.Replace("'", "''") + "'";
                    ventas = ventas.DefaultView.ToTable();
                }
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
                return;
            }

            subtitulo = "Del " + dtpDesde.Value.ToString("dd/MM/yyyy") + " al " + dtpHasta.Value.ToString("dd/MM/yyyy") +
                (cliente.Length > 0 ? "  |  Cliente: " + cliente : "") +
                "  |  Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            lblFacturas.Text = ventas.Rows.Count.ToString("N0");
            lblSubtotal.Text = ExportadorExcel.Suma(ventas, "Subtotal").ToString("C2");
            lblIva.Text = ExportadorExcel.Suma(ventas, "IVA").ToString("C2");
            lblTotal.Text = ExportadorExcel.Suma(ventas, "Total").ToString("C2");

            dgv.DataSource = ventas;
            Estilo.Columna(dgv, "Idfactura", "N.º", 25, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Cliente", "Cliente", 110, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Fecha", "Fecha", 60, "dd/MM/yyyy HH:mm", Estilo.Izquierda);
            Estilo.Columna(dgv, "Articulos", "Artículos", 35, "N0", Estilo.Derecha);
            Estilo.Columna(dgv, "Subtotal", "Subtotal", 45, "C2", Estilo.Derecha);
            Estilo.Columna(dgv, "IVA", "IVA", 40, "C2", Estilo.Derecha);
            Estilo.Columna(dgv, "Total", "Total", 45, "C2", Estilo.Derecha);

            btnExcel.Enabled = btnPdf.Enabled = ventas.Rows.Count > 0;
            if (ventas.Rows.Count == 0)
                Estilo.Info(this, "No hay facturas en el rango y filtro seleccionados.", "Sin resultados");
        }

        private void Exportar(string extension)
        {
            if (ventas == null || ventas.Rows.Count == 0) return;

            bool pdf = extension == "pdf";
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = pdf ? "Documento PDF (*.pdf)|*.pdf" : "Libro de Excel (*.xlsx)|*.xlsx";
                dlg.FileName = "Ventas_" + dtpDesde.Value.ToString("yyyyMMdd") + "_" + dtpHasta.Value.ToString("yyyyMMdd") + "." + extension;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    if (pdf) ExportadorPdf.Exportar(ventas, dlg.FileName, "Reporte de ventas", subtitulo);
                    else ExportadorExcel.Exportar(ventas, dlg.FileName, "Reporte de ventas", subtitulo);
                }
                catch (IOException)
                {
                    Estilo.Aviso(this, "No se pudo guardar el archivo.\nSi está abierto en otro programa, ciérralo e intenta de nuevo.", "Error al exportar");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Estilo.Aviso(this, "No tienes permiso para guardar en esa ubicación.", "Error al exportar");
                    return;
                }

                if (Estilo.Confirmar(this, "El reporte se exportó correctamente.\n¿Quieres abrirlo ahora?", "Exportación completa"))
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
        }
    }
}
