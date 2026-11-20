using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de productos (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmProductos : FrmCrudBase
    {
        private CampoTexto txtDescripcion, txtCantidad, txtPrecio;

        public FrmProductos(Modo modo) : base(modo, "producto", "productos") { }

        protected override string ColumnaId { get { return "Idproducto"; } }
        protected override string IconoEntidad { get { return Iconos.Productos; } }

        protected override Control CrearFormulario()
        {
            txtDescripcion = new CampoTexto
            {
                Etiqueta = "Descripción", Obligatorio = true, Placeholder = "Ej. Laptop 15 pulgadas",
                Ayuda = "Entre 3 y 100 caracteres.", Icono = Iconos.Productos, MaxLength = 100
            };
            txtCantidad = new CampoTexto
            {
                Etiqueta = "Cantidad en inventario", Obligatorio = true, Placeholder = "0",
                Ayuda = "Unidades disponibles (número entero).", MaxLength = 9
            };
            txtPrecio = new CampoTexto
            {
                Etiqueta = "Precio de venta", Obligatorio = true, Prefijo = "$", Placeholder = "0.00",
                Ayuda = "Usa punto para los decimales.", MaxLength = 12
            };

            Validador.Filtrar(txtCantidad, Filtro.Digitos);
            Validador.Filtrar(txtPrecio, Filtro.Decimal);
            validador.Agregar(txtDescripcion, Reglas.Texto("la descripción", 3, 100))
                     .Agregar(txtCantidad, Reglas.Entero("la cantidad", 0, 999999999))
                     .Agregar(txtPrecio, Reglas.Dinero("el precio"));

            return Apilar(
                new SeccionForm("Información del producto", "El nombre con el que aparecerá en las facturas.").Fila(txtDescripcion),
                new SeccionForm("Inventario y precio", "Existencias actuales y precio unitario.").Fila(txtCantidad, txtPrecio));
        }

        protected override DataTable Listar(string filtro) { return ProductoDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "Idproducto", "Id", 22, null, Estilo.Izquierda, 40);
            Estilo.Columna(dgv, "Descripcion", "Descripción", 150, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Cantidad", "Cantidad", 45, "N0", Estilo.Derecha);
            Estilo.Columna(dgv, "Precio", "Precio", 50, "C2", Estilo.Derecha);
        }

        protected override void CargarCampos(DataGridViewRow fila)
        {
            txtDescripcion.Text = (string)fila.Cells["Descripcion"].Value;
            txtCantidad.Text = fila.Cells["Cantidad"].Value.ToString();
            txtPrecio.Text = Convert.ToDouble(fila.Cells["Precio"].Value).ToString("0.00", CultureInfo.InvariantCulture);
        }

        protected override void LimpiarCampos()
        {
            txtDescripcion.Text = txtCantidad.Text = txtPrecio.Text = "";
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtDescripcion.Enabled = txtCantidad.Enabled = txtPrecio.Enabled = habilitado;
        }

        protected override void EnfocarPrimero() { if (txtDescripcion.Enabled) txtDescripcion.Caja.Focus(); }

        protected override string Guardar(bool nuevo)
        {
            string descripcion = txtDescripcion.Text.Trim();
            int cantidad = int.Parse(txtCantidad.Text.Trim(), CultureInfo.InvariantCulture);
            double precio = Reglas.ParseDecimal(txtPrecio.Text);
            return nuevo ? ProductoDatos.Insertar(descripcion, cantidad, precio)
                         : ProductoDatos.Actualizar(idActual, descripcion, cantidad, precio);
        }

        protected override bool MarcarErrorServidor(string error)
        {
            if (!error.Contains("descripción")) return false;
            validador.Marcar(txtDescripcion, error);
            return true;
        }

        protected override string EliminarRegistro(int id) { return ProductoDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return (string)fila.Cells["Descripcion"].Value; }
    }
}
