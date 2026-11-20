using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Consultar facturas (con su detalle) o eliminarlas devolviendo las existencias
    internal class FrmFacturas : Form
    {
        private readonly Modo modo;
        private readonly Validador validador = new Validador();
        private readonly CampoFecha dtpDesde, dtpHasta;
        private readonly CampoTexto txtBuscar;
        private readonly DataGridView dgvFacturas, dgvDetalle;
        private readonly BotonPildora btnEliminar;
        private readonly Tarjeta tarjetaDetalle;
        private readonly Label lblContador;

        public FrmFacturas(Modo modo)
        {
            this.modo = modo;
            bool eliminar = modo == Modo.Eliminar;
            Estilo.Hijo(this);
            string titulo = eliminar ? "Eliminar factura" : "Consultar facturas";
            Text = titulo;
            ClientSize = new Size(1040, 700);
            MinimumSize = new Size(900, 600);

            // Filtros
            dtpDesde = new CampoFecha { Etiqueta = "Desde", Ayuda = "Fecha inicial." };
            dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpHasta = new CampoFecha { Etiqueta = "Hasta", Ayuda = "Fecha final (incluida)." };
            dtpHasta.Value = DateTime.Today;
            txtBuscar = new CampoTexto { Etiqueta = "Cliente o N.º de factura", Placeholder = "Buscar...", Icono = Iconos.Buscar, Ayuda = "Opcional.", MaxLength = 100 };
            BotonPildora btnBuscar = new BotonPildora { Text = "Buscar", Icono = Iconos.Buscar, Width = 130 };
            BotonPildora btnLimpiar = new BotonPildora { Text = "Limpiar", Variante = Variante.Secundario, Width = 120, CausesValidation = false };
            Tarjeta filtros = new Tarjeta { Height = 134 };
            filtros.Controls.Add(Disposicion.Fila(new float[] { -170, -170, 100, -130, -120 }, dtpDesde, dtpHasta, txtBuscar,
                Disposicion.AlineadoACampo(btnBuscar), Disposicion.AlineadoACampo(btnLimpiar)));
            AcceptButton = btnBuscar;
            validador.AgregarRango(dtpDesde, dtpHasta);

            // Facturas
            dgvFacturas = Estilo.Tabla("No hay facturas en el rango seleccionado.");
            lblContador = new Label { AutoSize = false, Dock = DockStyle.Right, Width = 130, TextAlign = ContentAlignment.MiddleRight, ForeColor = Estilo.Gris, Font = Estilo.Pequena };
            Tarjeta tarjetaFacturas = new Tarjeta { Titulo = "Facturas", Icono = Iconos.Factura };
            if (eliminar)
            {
                btnEliminar = new BotonPildora { Text = "Eliminar factura", Icono = Iconos.Eliminar, Variante = Variante.Peligro, Width = 200, Enabled = false };
                btnEliminar.Click += btnEliminar_Click;
                FlowLayoutPanel acciones = new FlowLayoutPanel { Height = 58, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
                acciones.Controls.Add(btnEliminar);
                Label aviso = new Label
                {
                    AutoSize = true, ForeColor = Estilo.ErrorTexto, Font = Estilo.Pequena, Margin = new Padding(12, 14, 0, 0),
                    Text = "Se borrará su detalle y los productos volverán al inventario."
                };
                acciones.Controls.Add(aviso);
                tarjetaFacturas.Controls.Add(Disposicion.Pila(0, dgvFacturas, acciones));
            }
            else
            {
                tarjetaFacturas.Controls.Add(dgvFacturas);
            }

            // Detalle
            dgvDetalle = Estilo.Tabla("Selecciona una factura para ver su detalle.");
            tarjetaDetalle = new Tarjeta { Titulo = "Detalle de la factura", Icono = Iconos.Etiqueta };
            tarjetaDetalle.Controls.Add(dgvDetalle);

            TableLayoutPanel tablas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tablas.RowStyles.Add(new RowStyle(SizeType.Percent, eliminar ? 62 : 50));
            tablas.RowStyles.Add(new RowStyle(SizeType.Percent, eliminar ? 38 : 50));
            tarjetaFacturas.Dock = tarjetaDetalle.Dock = DockStyle.Fill;
            tablas.Controls.Add(tarjetaFacturas, 0, 0);
            tablas.Controls.Add(tarjetaDetalle, 0, 1);

            Panel cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };
            cuerpo.Controls.Add(Disposicion.Pila(1, filtros, tablas));
            Controls.Add(Disposicion.Pila(1, Estilo.Encabezado(Iconos.Factura, titulo,
                eliminar ? "Elige una factura y elimínala; sus productos regresan al inventario."
                         : "Filtra por fechas, cliente o número y revisa el detalle de cada factura."), cuerpo));

            btnBuscar.Click += (s, e) => Cargar();
            btnLimpiar.Click += (s, e) =>
            {
                dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpHasta.Value = DateTime.Today;
                txtBuscar.Text = "";
                validador.Limpiar();
                Cargar();
            };
            dgvFacturas.SelectionChanged += (s, e) => MostrarDetalle();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Cargar();
        }

        private void Cargar()
        {
            if (!validador.ValidarTodo())
            {
                Estilo.Aviso(this, "Corrige el rango de fechas para continuar.", "Rango inválido");
                return;
            }

            try
            {
                dgvFacturas.DataSource = FacturaDatos.Listar(dtpDesde.Value, dtpHasta.Value, txtBuscar.Text);
                Estilo.Columna(dgvFacturas, "Idfactura", "N.º", 22, null, Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Cliente", "Cliente", 110, null, Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Fecha", "Fecha", 62, "dd/MM/yyyy HH:mm", Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Articulos", "Artículos", 36, "N0", Estilo.Derecha);
                Estilo.Columna(dgvFacturas, "Subtotal", "Subtotal", 45, "C2", Estilo.Derecha);
                Estilo.Columna(dgvFacturas, "IVA", "IVA", 40, "C2", Estilo.Derecha);
                Estilo.Columna(dgvFacturas, "Total", "Total", 45, "C2", Estilo.Derecha);
                MostrarDetalle();
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        private int? FacturaSeleccionada()
        {
            if (dgvFacturas.SelectedRows.Count == 0) return null;
            return Convert.ToInt32(dgvFacturas.SelectedRows[0].Cells["Idfactura"].Value);
        }

        private void MostrarDetalle()
        {
            int? id = FacturaSeleccionada();
            if (btnEliminar != null) btnEliminar.Enabled = id.HasValue;

            if (!id.HasValue)
            {
                dgvDetalle.DataSource = null;
                tarjetaDetalle.Titulo = "Detalle de la factura";
                return;
            }

            try
            {
                tarjetaDetalle.Titulo = "Detalle de la factura N.º " + id.Value;
                dgvDetalle.DataSource = FacturaDatos.Detalle(id.Value);
                Estilo.Columna(dgvDetalle, "Iddetalle", "Id", 18, null, Estilo.Izquierda, 40);
                Estilo.Columna(dgvDetalle, "Descripcion", "Producto", 150, null, Estilo.Izquierda);
                Estilo.Columna(dgvDetalle, "cantidad", "Cantidad", 40, "N0", Estilo.Derecha);
                Estilo.Columna(dgvDetalle, "Precio", "Precio", 50, "C2", Estilo.Derecha);
                Estilo.Columna(dgvDetalle, "Total", "Total", 55, "C2", Estilo.Derecha);
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            int? id = FacturaSeleccionada();
            if (!id.HasValue) return;

            if (!Estilo.Confirmar(this, "Se borrará el detalle de la factura N.º " + id.Value + " y los productos regresarán al inventario.\nEsta acción no se puede deshacer.",
                    "¿Eliminar la factura?", "Sí, eliminar", true))
                return;

            try
            {
                string error = FacturaDatos.Eliminar(id.Value);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo eliminar");
                    return;
                }
                Estilo.Exito(this, "La factura se eliminó y las existencias fueron restituidas.", "Factura eliminada");
                Cargar();
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }
    }
}
