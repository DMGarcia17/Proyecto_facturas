using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Reporte de ventas por rango de fechas, con filtro por cliente, exportable a Excel y PDF
    internal class FrmReporteVentas : Form
    {
        private const string TodosLosClientes = "(Todos los clientes)";

        private readonly Validador validador = new Validador();
        private readonly CampoFecha dtpDesde, dtpHasta;
        private readonly CampoLista cboCliente;
        private readonly TarjetaResumen tFacturas, tSubtotal, tIva, tTotal;
        private readonly BotonPildora btnExcel, btnPdf;
        private readonly DataGridView dgv;
        private DataTable ventas;
        private string subtitulo = "";

        public FrmReporteVentas()
        {
            Estilo.Hijo(this);
            Text = "Reporte de ventas";
            ClientSize = new Size(1040, 700);
            MinimumSize = new Size(900, 600);

            // Filtros
            dtpDesde = new CampoFecha { Etiqueta = "Desde", Ayuda = "Fecha inicial." };
            dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpHasta = new CampoFecha { Etiqueta = "Hasta", Ayuda = "Fecha final (incluida)." };
            dtpHasta.Value = DateTime.Today;
            cboCliente = new CampoLista { Etiqueta = "Cliente", Ayuda = "Filtra las ventas de un solo cliente." };
            BotonPildora btnGenerar = new BotonPildora { Text = "Generar reporte", Icono = Iconos.Reporte, Width = 190 };
            Tarjeta filtros = new Tarjeta { Height = 134 };
            filtros.Controls.Add(Disposicion.Fila(new float[] { -170, -170, 100, -190 }, dtpDesde, dtpHasta, cboCliente, Disposicion.AlineadoACampo(btnGenerar)));
            AcceptButton = btnGenerar;
            validador.AgregarRango(dtpDesde, dtpHasta);

            // Tarjetas de resumen pastel
            tFacturas = Resumen("Facturas emitidas", Iconos.Factura, Estilo.InfoClaro, Estilo.InfoTexto);
            tSubtotal = Resumen("Subtotal", Iconos.Etiqueta, Estilo.PrimarioClaro, Estilo.Primario);
            tIva = Resumen("IVA", Iconos.Reporte, Estilo.AvisoClaro, Estilo.AvisoTexto);
            tTotal = Resumen("Total vendido", Iconos.Check, Estilo.ExitoClaro, Estilo.ExitoOscuro);
            TableLayoutPanel tarjetas = new TableLayoutPanel
            {
                Height = 100, ColumnCount = 4, RowCount = 1, Padding = new Padding(8, 8, 8, 8), Margin = Padding.Empty
            };
            for (int i = 0; i < 4; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tarjetas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            TarjetaResumen[] todas = { tFacturas, tSubtotal, tIva, tTotal };
            for (int i = 0; i < 4; i++)
            {
                todas[i].Dock = DockStyle.Fill;
                todas[i].Margin = new Padding(0, 0, i < 3 ? 14 : 0, 0);
                tarjetas.Controls.Add(todas[i], i, 0);
            }

            // Tabla + exportacion
            dgv = Estilo.Tabla("No hay facturas en el rango y filtro seleccionados.");
            btnPdf = new BotonPildora { Text = "Exportar a PDF", Icono = Iconos.Descargar, Width = 190, Enabled = false };
            btnExcel = new BotonPildora { Text = "Exportar a Excel", Icono = Iconos.Descargar, Variante = Variante.Exito, Width = 200, Enabled = false };
            FlowLayoutPanel acciones = new FlowLayoutPanel { Height = 58, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
            acciones.Controls.Add(btnPdf);
            btnPdf.Margin = new Padding(12, 0, 0, 0);
            acciones.Controls.Add(btnExcel);
            Tarjeta tarjetaTabla = new Tarjeta { Titulo = "Facturas del período", Icono = Iconos.Factura };
            tarjetaTabla.Controls.Add(Disposicion.Pila(0, dgv, acciones));

            Panel cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };
            cuerpo.Controls.Add(Disposicion.Pila(2, filtros, tarjetas, tarjetaTabla));
            Controls.Add(Disposicion.Pila(1, Estilo.Encabezado(Iconos.Reporte, "Reporte de ventas", "Ventas por rango de fechas, con exportación a Excel y PDF."), cuerpo));

            btnGenerar.Click += (s, e) => Generar();
            btnExcel.Click += (s, e) => Exportar("xlsx");
            btnPdf.Click += (s, e) => Exportar("pdf");
        }

        private static TarjetaResumen Resumen(string titulo, string icono, Color fondo, Color acento)
        {
            TarjetaResumen t = new TarjetaResumen { Titulo = titulo, Icono = icono };
            t.Colores(fondo, acento);
            return t;
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
                Estilo.Aviso(this, "Corrige el rango de fechas para generar el reporte.", "Rango inválido");
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

            tFacturas.Valor = ventas.Rows.Count.ToString("N0");
            tSubtotal.Valor = ExportadorExcel.Suma(ventas, "Subtotal").ToString("C2");
            tIva.Valor = ExportadorExcel.Suma(ventas, "IVA").ToString("C2");
            tTotal.Valor = ExportadorExcel.Suma(ventas, "Total").ToString("C2");

            dgv.DataSource = ventas;
            Estilo.Columna(dgv, "Idfactura", "N.º", 22, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Cliente", "Cliente", 110, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Fecha", "Fecha", 62, "dd/MM/yyyy HH:mm", Estilo.Izquierda);
            Estilo.Columna(dgv, "Articulos", "Artículos", 36, "N0", Estilo.Derecha);
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
                    Dialogo.Error(this, "No se pudo guardar el archivo", "Si está abierto en otro programa, ciérralo e intenta de nuevo.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Dialogo.Error(this, "No se pudo guardar el archivo", "No tienes permiso para guardar en esa ubicación.");
                    return;
                }

                if (Estilo.Confirmar(this, "El reporte se exportó correctamente. ¿Quieres abrirlo ahora?", "Exportación completa", "Abrir archivo"))
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
        }
    }
}
