using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Crea una factura en tres pasos: 1) cliente, 2) productos, 3) resumen. Al generarla se crean la factura
    // y su detalle (un registro de Detallefactura por producto) y se descuentan las existencias.
    internal class FrmFacturaNueva : Form
    {
        private readonly Validador validador = new Validador();
        private readonly Validador lineaValidador = new Validador { AlSalir = false }; // producto y cantidad: solo al pulsar "Agregar"
        private readonly CampoLista cboCliente, cboProducto;
        private readonly CampoTexto txtCantidad;
        private readonly Label lblInfoCliente, lblFecha, lblSubtotal, lblIva, lblTotal;
        private readonly DataGridView dgv;
        private readonly BotonPildora btnQuitar, btnGuardar;
        private readonly DataTable lineas = new DataTable();
        private DataTable productos;

        public FrmFacturaNueva()
        {
            Estilo.Hijo(this);
            Text = "Nueva factura";
            ClientSize = new Size(1040, 700);
            MinimumSize = new Size(900, 620);

            lineas.Columns.Add("Idproducto", typeof(int));
            lineas.Columns.Add("Descripcion", typeof(string));
            lineas.Columns.Add("Cantidad", typeof(int));
            lineas.Columns.Add("Precio", typeof(double));
            lineas.Columns.Add("Total", typeof(double));

            // ---- 1) Cliente ----
            cboCliente = new CampoLista { Etiqueta = "Cliente", Obligatorio = true, Ayuda = "A quién se le emite la factura." };
            lblInfoCliente = new Label { AutoSize = false, Height = 24, ForeColor = Estilo.Gris, Font = Estilo.Pequena, TextAlign = ContentAlignment.MiddleLeft };
            Tarjeta tarjetaCliente = new Tarjeta { Titulo = "Cliente", Numero = 1, Height = 206 };
            tarjetaCliente.Controls.Add(Disposicion.Pila(-1, cboCliente, lblInfoCliente));

            // ---- 2) Productos ----
            cboProducto = new CampoLista { Etiqueta = "Producto", Obligatorio = true, Ayuda = "Solo con existencias." };
            txtCantidad = new CampoTexto { Etiqueta = "Cantidad", Obligatorio = true, Placeholder = "1", MaxLength = 9, Ayuda = "Elige un producto." };
            BotonPildora btnAgregar = new BotonPildora { Text = "Agregar", Icono = Iconos.Agregar, Width = 130 };
            Validador.Filtrar(txtCantidad, Filtro.Digitos);

            dgv = Estilo.Tabla("Aún no has agregado productos a la factura.");
            // Las columnas se generan al mostrar la tabla, por eso el formato se aplica al terminar el enlace
            dgv.DataBindingComplete += (s, e) =>
            {
                Estilo.Columna(dgv, "Idproducto", "Id", 18, null, Estilo.Izquierda, 40);
                Estilo.Columna(dgv, "Descripcion", "Producto", 150, null, Estilo.Izquierda);
                Estilo.Columna(dgv, "Cantidad", "Cantidad", 40, "N0", Estilo.Derecha);
                Estilo.Columna(dgv, "Precio", "Precio", 45, "C2", Estilo.Derecha);
                Estilo.Columna(dgv, "Total", "Total", 50, "C2", Estilo.Derecha);
            };
            dgv.DataSource = lineas;

            btnQuitar = new BotonPildora { Text = "Quitar producto", Icono = Iconos.Eliminar, Variante = Variante.Secundario, Width = 180, Enabled = false, CausesValidation = false };
            Panel filaQuitar = new Panel { Height = 56, Padding = new Padding(0, 12, 0, 0) };
            btnQuitar.Location = new Point(0, 12);
            filaQuitar.Controls.Add(btnQuitar);

            Control filaAgregar = Disposicion.Fila(new float[] { 100, -170, -140 }, cboProducto, txtCantidad, Disposicion.AlineadoACampo(btnAgregar));
            Tarjeta tarjetaProductos = new Tarjeta { Titulo = "Productos", Numero = 2 };
            tarjetaProductos.Controls.Add(Disposicion.Pila(1, filaAgregar, dgv, filaQuitar));

            TableLayoutPanel izquierda = Disposicion.Pila(1, tarjetaCliente, tarjetaProductos);

            // ---- 3) Resumen ----
            Tarjeta tarjetaResumen = new Tarjeta { Titulo = "Resumen", Numero = 3, Dock = DockStyle.Fill };
            lblFecha = new Label(); lblSubtotal = new Label(); lblIva = new Label(); lblTotal = new Label();
            Panel spacer = new Panel { BackColor = Color.White };
            btnGuardar = new BotonPildora { Text = "Generar factura", Icono = Iconos.Check, Variante = Variante.Exito, Height = 46, Margin = new Padding(0, 0, 0, 8) };
            BotonPildora btnLimpiar = new BotonPildora { Text = "Limpiar todo", Icono = Iconos.Limpiar, Variante = Variante.Secundario, Height = 42, CausesValidation = false };
            tarjetaResumen.Controls.Add(Disposicion.Pila(7,
                Linea("Fecha", lblFecha, false), Linea("Factura N.º", new Label { Text = "se asigna al guardar" }, false), Separador(),
                Linea("Subtotal", lblSubtotal, false), Linea("IVA (" + Conexion.IvaPorcentaje.ToString("0.##") + "%)", lblIva, false), Separador(),
                Linea("Total a pagar", lblTotal, true), spacer, btnGuardar, btnLimpiar));

            TableLayoutPanel cuerpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12), ColumnCount = 2, RowCount = 1
            };
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            cuerpo.Controls.Add(izquierda, 0, 0);
            cuerpo.Controls.Add(tarjetaResumen, 1, 0);
            Controls.Add(Disposicion.Pila(1, Estilo.Encabezado(Iconos.Factura, "Nueva factura", "Elige el cliente, agrega los productos y genera la factura."), cuerpo));

            // ---- Reglas de validacion (todas del helper) ----
            validador.Agregar(cboCliente, "un cliente");
            lineaValidador.Agregar(cboProducto, "un producto");
            lineaValidador.Agregar(txtCantidad, Reglas.Entero("la cantidad", 1, StockDisponible));

            cboCliente.ValorCambiado += (s, e) => MostrarInfoCliente();
            cboProducto.ValorCambiado += (s, e) => ActualizarAyudaCantidad();
            btnAgregar.Click += btnAgregar_Click;
            btnQuitar.Click += btnQuitar_Click;
            btnGuardar.Click += btnGuardar_Click;
            btnLimpiar.Click += (s, e) => Reiniciar();
            dgv.SelectionChanged += (s, e) => btnQuitar.Enabled = dgv.SelectedRows.Count > 0;
            AcceptButton = btnAgregar;

            lblFecha.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Recalcular();
        }

        // Fila "titulo ........ valor" del resumen
        private static Panel Linea(string titulo, Label valor, bool grande)
        {
            Panel p = new Panel { Height = grande ? 56 : 36, BackColor = Color.White };
            Label t = new Label
            {
                Text = titulo, AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = grande ? Estilo.Texto : Estilo.Gris, Font = grande ? Estilo.Subtitulo : Estilo.Normal
            };
            valor.AutoSize = true;
            valor.Dock = DockStyle.Right;
            valor.TextAlign = ContentAlignment.MiddleRight;
            valor.ForeColor = grande ? Estilo.Primario : Estilo.Texto;
            valor.Font = grande ? new Font("Segoe UI Semibold", 18F) : Estilo.Normal;
            p.Controls.Add(valor);
            p.Controls.Add(t);
            return p;
        }

        private static Panel Separador()
        {
            return new Panel { Height = 1, BackColor = Estilo.Borde, Margin = new Padding(0, 6, 0, 6) };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CargarCatalogos();
            cboCliente.Interior.Focus();
        }

        private void CargarCatalogos()
        {
            try
            {
                cboCliente.DataSource = ClienteDatos.Listar("");
                cboCliente.DisplayMember = "Nombre";
                cboCliente.ValueMember = "Nombre";
                cboCliente.SelectedIndex = -1;

                productos = ProductoDatos.Disponibles();
                cboProducto.DataSource = productos;
                cboProducto.DisplayMember = "Texto";
                cboProducto.ValueMember = "Idproducto";
                cboProducto.SelectedIndex = -1;
                MostrarInfoCliente();
                ActualizarAyudaCantidad();
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        private void MostrarInfoCliente()
        {
            DataRowView c = cboCliente.SelectedItem as DataRowView;
            if (c == null) { lblInfoCliente.Text = ""; return; }
            string correo = c["Email"] == DBNull.Value ? "sin correo" : (string)c["Email"];
            lblInfoCliente.Text = "Tel. " + c["Telefono"] + "   ·   " + correo;
        }

        private void ActualizarAyudaCantidad()
        {
            txtCantidad.Ayuda = cboProducto.SelectedIndex < 0 ? "Elige un producto." : "Disponibles: " + StockDisponible().ToString("N0");
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
            bool ok = lineaValidador.Validar(cboProducto);
            ok = ok & lineaValidador.Validar(txtCantidad);
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

            txtCantidad.Text = "";
            cboProducto.SelectedIndex = -1;
            lineaValidador.Limpiar();
            cboProducto.Interior.Focus();
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
            ActualizarAyudaCantidad();
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
            if (!validador.ValidarTodo())
            {
                Estilo.Aviso(this, "Selecciona el cliente de la factura.", "Falta el cliente");
                return;
            }
            if (lineas.Rows.Count == 0)
            {
                Estilo.Aviso(this, "Agrega al menos un producto a la factura.", "Factura vacía");
                cboProducto.Interior.Focus();
                return;
            }

            if (!Estilo.Confirmar(this, "Se generará la factura para " + cboCliente.Text + " por " + lblTotal.Text + ".",
                    "¿Generar la factura?", "Sí, generar"))
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

                Estilo.Exito(this, "La factura N.º " + idFactura + " se generó por " + lblTotal.Text + ".", "Factura generada");
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
            txtCantidad.Text = "";
            lblFecha.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            Recalcular();
            CargarCatalogos();
            validador.Limpiar();
            lineaValidador.Limpiar();
            cboCliente.Interior.Focus();
        }
    }
}
