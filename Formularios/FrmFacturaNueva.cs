using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Crea una factura: elige el cliente, agrega productos y al guardar se generan la factura
    // y su detalle (un registro de Detallefactura por producto) descontando las existencias.
    internal class FrmFacturaNueva : Form
    {
        private readonly Validador validador = new Validador();
        private readonly Validador lineaValidador = new Validador();
        private readonly ComboBox cboCliente, cboProducto;
        private readonly TextBox txtCantidad;
        private readonly DataGridView dgv;
        private readonly Label lblSubtotal, lblIva, lblTotal, lblFecha, lblNumero;
        private readonly Button btnQuitar, btnGuardar;
        private readonly DataTable lineas = new DataTable();
        private DataTable productos;

        public FrmFacturaNueva()
        {
            Estilo.Hijo(this);
            Text = "Nueva factura";
            ClientSize = new Size(980, 680);
            MinimumSize = new Size(980, 680);
            Estilo.Encabezado(this, "Nueva factura", "Elige el cliente, agrega los productos y genera la factura");

            lineas.Columns.Add("Idproducto", typeof(int));
            lineas.Columns.Add("Descripcion", typeof(string));
            lineas.Columns.Add("Cantidad", typeof(int));
            lineas.Columns.Add("Precio", typeof(double));
            lineas.Columns.Add("Total", typeof(double));

            // Cliente y datos de la factura
            Estilo.NuevaEtiqueta(this, "Cliente", 24, 106);
            cboCliente = Estilo.NuevaCombo(this, 24, 130, 440);
            Estilo.NuevaEtiqueta(this, "Fecha", 484, 106);
            lblFecha = new Label { AutoSize = true, Font = Estilo.Normal, Location = new Point(484, 134), Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm") };
            Controls.Add(lblFecha);
            Estilo.NuevaEtiqueta(this, "Factura N.º", 700, 106);
            lblNumero = new Label { AutoSize = true, Font = Estilo.Subtitulo, ForeColor = Estilo.Gris, Location = new Point(700, 132), Text = "se asigna al guardar" };
            Controls.Add(lblNumero);

            // Agregar producto
            Estilo.NuevaEtiqueta(this, "Producto", 24, 180);
            cboProducto = Estilo.NuevaCombo(this, 24, 204, 600);
            Estilo.NuevaEtiqueta(this, "Cantidad", 644, 180);
            txtCantidad = Estilo.NuevaCaja(this, 644, 204, 110, 9);
            Button btnAgregar = Estilo.NuevoBoton(this, "Agregar", 774, 196, 130, true);
            Validador.Filtrar(txtCantidad, Filtro.Digitos);

            // Tabla de lineas
            dgv = Estilo.NuevaTabla(this, 24, 258, 932, 270);
            dgv.DataSource = lineas;
            Estilo.Columna(dgv, "Idproducto", "Id", 20, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Descripcion", "Producto", 150, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Cantidad", "Cantidad", 40, "N0", Estilo.Derecha);
            Estilo.Columna(dgv, "Precio", "Precio", 50, "C2", Estilo.Derecha);
            Estilo.Columna(dgv, "Total", "Total", 55, "C2", Estilo.Derecha);

            // Totales
            Panel totales = new Panel { Size = new Size(300, 96), BackColor = Color.White, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            totales.Location = new Point(ClientSize.Width - 24 - 300, 538);
            lblSubtotal = FilaTotal(totales, "Subtotal", 8, false);
            lblIva = FilaTotal(totales, "IVA (" + Conexion.IvaPorcentaje.ToString("0.##") + "%)", 34, false);
            lblTotal = FilaTotal(totales, "Total", 62, true);
            Controls.Add(totales);

            btnQuitar = Estilo.NuevoBoton(this, "Quitar producto", 24, 546, 160, false);
            btnQuitar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnQuitar.Enabled = false;
            btnGuardar = Estilo.NuevoBoton(this, "Generar factura", 24, 594, 180, true);
            btnGuardar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Button btnLimpiar = Estilo.NuevoBoton(this, "Limpiar", 214, 594, 110, false);
            btnLimpiar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLimpiar.CausesValidation = false;

            // Reglas de validacion (todas del helper)
            validador.Agregar(cboCliente, "un cliente");
            lineaValidador.AlSalir = false; // el producto y la cantidad se validan solo al pulsar "Agregar"
            lineaValidador.Agregar(cboProducto, "un producto");
            lineaValidador.Agregar(txtCantidad, Reglas.Entero("la cantidad", 1, StockDisponible));

            btnAgregar.Click += btnAgregar_Click;
            btnQuitar.Click += btnQuitar_Click;
            btnGuardar.Click += btnGuardar_Click;
            btnLimpiar.Click += (s, e) => Reiniciar();
            dgv.SelectionChanged += (s, e) => btnQuitar.Enabled = dgv.SelectedRows.Count > 0;
            AcceptButton = btnAgregar;
        }

        private Label FilaTotal(Panel padre, string titulo, int y, bool grande)
        {
            padre.Controls.Add(new Label
            {
                AutoSize = true, Text = titulo, Location = new Point(14, y + (grande ? 4 : 2)),
                Font = grande ? Estilo.Subtitulo : Estilo.Normal, ForeColor = grande ? Estilo.Texto : Estilo.Gris
            });
            Label valor = new Label
            {
                AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Text = 0.ToString("C2"),
                Location = new Point(110, y), Size = new Size(176, 26),
                Font = grande ? new Font("Segoe UI Semibold", 14F) : Estilo.Normal,
                ForeColor = grande ? Estilo.Primario : Estilo.Texto
            };
            padre.Controls.Add(valor);
            return valor;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CargarCatalogos();
        }

        private void CargarCatalogos()
        {
            try
            {
                DataTable clientes = ClienteDatos.Nombres();
                cboCliente.DataSource = clientes;
                cboCliente.DisplayMember = "Nombre";
                cboCliente.ValueMember = "Nombre";
                cboCliente.SelectedIndex = -1;

                productos = ProductoDatos.Disponibles();
                cboProducto.DataSource = productos;
                cboProducto.DisplayMember = "Texto";
                cboProducto.ValueMember = "Idproducto";
                cboProducto.SelectedIndex = -1;
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        // Existencias del producto elegido menos lo ya agregado a la factura
        private int StockDisponible()
        {
            DataRowView p = cboProducto.SelectedItem as DataRowView;
            if (p == null) return 0;
            int yaAgregado = lineas.AsEnumerable().Where(l => l.Field<int>("Idproducto") == (int)p["Idproducto"]).Sum(l => l.Field<int>("Cantidad"));
            return Math.Max(0, (int)p["Cantidad"] - yaAgregado);
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            bool ok = lineaValidador.Validar(cboProducto) & lineaValidador.Validar(txtCantidad);
            if (!ok) return;

            DataRowView p = (DataRowView)cboProducto.SelectedItem;
            int id = (int)p["Idproducto"];
            int cantidad = int.Parse(txtCantidad.Text.Trim());
            double precio = Convert.ToDouble(p["Precio"]);

            DataRow existente = lineas.AsEnumerable().FirstOrDefault(l => l.Field<int>("Idproducto") == id);
            if (existente != null)
            {
                existente["Cantidad"] = (int)existente["Cantidad"] + cantidad;
                existente["Total"] = Redondear(precio * (int)existente["Cantidad"]);
            }
            else
            {
                lineas.Rows.Add(id, p["Descripcion"], cantidad, precio, Redondear(precio * cantidad));
            }

            txtCantidad.Clear();
            cboProducto.SelectedIndex = -1;
            lineaValidador.Limpiar();
            cboProducto.Focus();
            Recalcular();
        }

        private void btnQuitar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            DataRowView fila = dgv.SelectedRows[0].DataBoundItem as DataRowView;
            if (fila == null) return;
            fila.Row.Delete();
            lineas.AcceptChanges();
            Recalcular();
        }

        private void Recalcular()
        {
            double subtotal = Redondear(lineas.AsEnumerable().Sum(l => l.Field<double>("Total")));
            double iva = Redondear(subtotal * Conexion.IvaPorcentaje / 100.0);
            lblSubtotal.Text = subtotal.ToString("C2");
            lblIva.Text = iva.ToString("C2");
            lblTotal.Text = Redondear(subtotal + iva).ToString("C2");
        }

        private static double Redondear(double v)
        {
            return Math.Round(v, 2, MidpointRounding.AwayFromZero);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!validador.Validar(cboCliente))
            {
                Estilo.Aviso(this, "Selecciona el cliente de la factura.", "Datos incompletos");
                return;
            }
            if (lineas.Rows.Count == 0)
            {
                Estilo.Aviso(this, "Agrega al menos un producto a la factura.", "Datos incompletos");
                cboProducto.Focus();
                return;
            }

            if (!Estilo.Confirmar(this, "¿Generar la factura para " + cboCliente.Text + " por " + lblTotal.Text + "?", "Confirmar factura"))
                return;

            try
            {
                var detalle = lineas.AsEnumerable().Select(l => new LineaFactura
                {
                    IdProducto = l.Field<int>("Idproducto"),
                    Cantidad = l.Field<int>("Cantidad")
                }).ToList();

                string error = FacturaDatos.Crear((string)cboCliente.SelectedValue, detalle, out int idFactura);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo generar la factura");
                    CargarCatalogos(); // las existencias pudieron cambiar
                    return;
                }

                Estilo.Info(this, "La factura N.º " + idFactura + " se generó correctamente.\nTotal: " + lblTotal.Text, "Factura generada");
                Reiniciar();
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        // Deja la pantalla lista para una factura nueva
        private void Reiniciar()
        {
            lineas.Clear();
            txtCantidad.Clear();
            lblFecha.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Recalcular();
            CargarCatalogos();
            validador.Limpiar();
            lineaValidador.Limpiar();
            cboCliente.Focus();
        }
    }
}
