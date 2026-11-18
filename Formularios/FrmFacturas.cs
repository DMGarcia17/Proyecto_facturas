using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Consultar facturas (con su detalle) o eliminarlas devolviendo las existencias
    internal class FrmFacturas : Form
    {
        private readonly Modo modo;
        private readonly Validador validador = new Validador();
        private readonly DateTimePicker dtpDesde, dtpHasta;
        private readonly TextBox txtBuscar;
        private readonly DataGridView dgvFacturas, dgvDetalle;
        private readonly Button btnEliminar;
        private readonly Label lblDetalle;

        public FrmFacturas(Modo modo)
        {
            this.modo = modo;
            Estilo.Hijo(this);
            string titulo = modo == Modo.Eliminar ? "Eliminar factura" : "Consultar facturas";
            Text = titulo;
            ClientSize = new Size(980, 680);
            MinimumSize = new Size(980, 680);
            Estilo.Encabezado(this, titulo, modo == Modo.Eliminar
                ? "Selecciona una factura y elimínala; sus productos regresan al inventario"
                : "Filtra por fechas, cliente o número y revisa el detalle de cada factura");

            Estilo.NuevaEtiqueta(this, "Desde", 24, 106);
            dtpDesde = Estilo.NuevaFecha(this, 24, 130, 150);
            dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Estilo.NuevaEtiqueta(this, "Hasta", 194, 106);
            dtpHasta = Estilo.NuevaFecha(this, 194, 130, 150);
            dtpHasta.Value = DateTime.Today;
            Estilo.NuevaEtiqueta(this, "Cliente o N.º de factura", 364, 106);
            txtBuscar = Estilo.NuevaCaja(this, 364, 130, 300, 100);
            Button btnBuscar = Estilo.NuevoBoton(this, "Buscar", 684, 122, 120, true);
            Button btnTodas = Estilo.NuevoBoton(this, "Limpiar", 814, 122, 120, false);
            btnTodas.CausesValidation = false;
            AcceptButton = btnBuscar;

            validador.AgregarRango(dtpDesde, dtpHasta);

            Label l1 = Estilo.NuevaEtiqueta(this, "Facturas", 24, 178);
            l1.Font = Estilo.Subtitulo;
            dgvFacturas = Estilo.NuevaTabla(this, 24, 206, 932, 260);
            dgvFacturas.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblDetalle = Estilo.NuevaEtiqueta(this, "Detalle de la factura", 24, 478);
            lblDetalle.Font = Estilo.Subtitulo;
            lblDetalle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            int altoDetalle = modo == Modo.Eliminar ? 110 : 150;
            dgvDetalle = Estilo.NuevaTabla(this, 24, 506, 932, altoDetalle);
            dgvDetalle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            if (modo == Modo.Eliminar)
            {
                btnEliminar = Estilo.NuevoBoton(this, "Eliminar factura", 24, 628, 170, false);
                Estilo.BotonPeligro(btnEliminar);
                btnEliminar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                btnEliminar.Enabled = false;
                btnEliminar.Click += btnEliminar_Click;
            }

            btnBuscar.Click += (s, e) => Cargar();
            btnTodas.Click += (s, e) =>
            {
                dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpHasta.Value = DateTime.Today;
                txtBuscar.Clear();
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
                Estilo.Aviso(this, "Corrige el rango de fechas.", "Datos incompletos");
                return;
            }

            try
            {
                dgvFacturas.DataSource = FacturaDatos.Listar(dtpDesde.Value, dtpHasta.Value, txtBuscar.Text);
                Estilo.Columna(dgvFacturas, "Idfactura", "N.º", 25, null, Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Cliente", "Cliente", 110, null, Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Fecha", "Fecha", 60, "dd/MM/yyyy HH:mm", Estilo.Izquierda);
                Estilo.Columna(dgvFacturas, "Articulos", "Artículos", 35, "N0", Estilo.Derecha);
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
                lblDetalle.Text = "Detalle de la factura";
                return;
            }

            try
            {
                lblDetalle.Text = "Detalle de la factura N.º " + id.Value;
                dgvDetalle.DataSource = FacturaDatos.Detalle(id.Value);
                Estilo.Columna(dgvDetalle, "Iddetalle", "Id", 20, null, Estilo.Izquierda);
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

            if (!Estilo.Confirmar(this, "¿Seguro que quieres eliminar la factura N.º " + id.Value + "?\n" +
                "Se borrará su detalle y los productos regresarán al inventario.\nEsta acción no se puede deshacer.",
                "Confirmar eliminación"))
                return;

            try
            {
                string error = FacturaDatos.Eliminar(id.Value);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo eliminar");
                    return;
                }
                Estilo.Info(this, "La factura se eliminó y las existencias fueron restituidas.", "Factura eliminada");
                Cargar();
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }
    }
}
