using System.Data;
using System.Globalization;
using System.Windows.Forms;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de productos (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmProductos : FrmCrudBase
    {
        private TextBox txtDescripcion, txtCantidad, txtPrecio;

        public FrmProductos(Modo modo) : base(modo, "producto", "productos") { }

        protected override string ColumnaId { get { return "Idproducto"; } }

        protected override int CrearCampos(int y)
        {
            txtDescripcion = Campo("Descripción", 24, y, 440, 100);
            txtCantidad = Campo("Cantidad", 484, y, 190, 9);
            txtPrecio = Campo("Precio (USD)", 694, y, 222, 12);

            Validador.Filtrar(txtCantidad, Filtro.Digitos);
            Validador.Filtrar(txtPrecio, Filtro.Decimal);
            validador.Agregar(txtDescripcion, Reglas.Texto("la descripción", 3, 100))
                     .Agregar(txtCantidad, Reglas.Entero("la cantidad", 0, 999999999))
                     .Agregar(txtPrecio, Reglas.Dinero("el precio"));
            return 66;
        }

        protected override DataTable Listar(string filtro) { return ProductoDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "Idproducto", "Id", 25, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Descripcion", "Descripción", 150, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Cantidad", "Cantidad", 50, "N0", Estilo.Derecha);
            Estilo.Columna(dgv, "Precio", "Precio", 55, "C2", Estilo.Derecha);
        }

        protected override void CargarCampos(DataGridViewRow fila)
        {
            txtDescripcion.Text = (string)fila.Cells["Descripcion"].Value;
            txtCantidad.Text = fila.Cells["Cantidad"].Value.ToString();
            txtPrecio.Text = System.Convert.ToDouble(fila.Cells["Precio"].Value).ToString("0.00", CultureInfo.InvariantCulture);
        }

        protected override void LimpiarCampos()
        {
            txtDescripcion.Clear(); txtCantidad.Clear(); txtPrecio.Clear();
            if (txtDescripcion.Enabled) txtDescripcion.Focus();
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtDescripcion.Enabled = txtCantidad.Enabled = txtPrecio.Enabled = habilitado;
        }

        protected override string Guardar(bool nuevo)
        {
            string descripcion = txtDescripcion.Text.Trim();
            int cantidad = int.Parse(txtCantidad.Text.Trim(), CultureInfo.InvariantCulture);
            double precio = Reglas.ParseDecimal(txtPrecio.Text);
            return nuevo ? ProductoDatos.Insertar(descripcion, cantidad, precio)
                         : ProductoDatos.Actualizar(idActual, descripcion, cantidad, precio);
        }

        protected override string EliminarRegistro(int id) { return ProductoDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return (string)fila.Cells["Descripcion"].Value; }
    }
}
