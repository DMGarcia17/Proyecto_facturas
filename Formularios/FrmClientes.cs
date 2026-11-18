using System;
using System.Data;
using System.Windows.Forms;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de clientes (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmClientes : FrmCrudBase
    {
        private TextBox txtNombre, txtTelefono, txtEmail, txtDireccion;

        public FrmClientes(Modo modo) : base(modo, "cliente", "clientes") { }

        protected override string ColumnaId { get { return "Idcliente"; } }

        protected override int CrearCampos(int y)
        {
            txtNombre = Campo("Nombre", 24, y, 440, 100);
            txtTelefono = Campo("Teléfono", 484, y, 190, 20);
            txtEmail = Campo("Correo electrónico (opcional)", 694, y, 222, 150);
            txtDireccion = Campo("Dirección (opcional)", 24, y + 66, 892, 255);

            Validador.Filtrar(txtTelefono, Filtro.Telefono);
            validador.Agregar(txtNombre, Reglas.Texto("el nombre", 3, 100))
                     .Agregar(txtTelefono, Reglas.Telefono())
                     .Agregar(txtEmail, Reglas.Opcional(Reglas.Email()))
                     .Agregar(txtDireccion, Reglas.Opcional(Reglas.Texto("la dirección", 5, 255, false)));
            return 132;
        }

        protected override DataTable Listar(string filtro) { return ClienteDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "Idcliente", "Id", 25, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Nombre", "Nombre", 110, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Telefono", "Teléfono", 50, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Email", "Correo electrónico", 90, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Direccion", "Dirección", 110, null, Estilo.Izquierda);
        }

        protected override void CargarCampos(DataGridViewRow fila)
        {
            txtNombre.Text = Texto(fila, "Nombre");
            txtTelefono.Text = Texto(fila, "Telefono");
            txtEmail.Text = Texto(fila, "Email");
            txtDireccion.Text = Texto(fila, "Direccion");
        }

        private static string Texto(DataGridViewRow fila, string columna)
        {
            object v = fila.Cells[columna].Value;
            return v == null || v == DBNull.Value ? "" : (string)v;
        }

        protected override void LimpiarCampos()
        {
            txtNombre.Clear(); txtTelefono.Clear(); txtEmail.Clear(); txtDireccion.Clear();
            if (txtNombre.Enabled) txtNombre.Focus();
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtNombre.Enabled = txtTelefono.Enabled = txtEmail.Enabled = txtDireccion.Enabled = habilitado;
        }

        protected override string Guardar(bool nuevo)
        {
            string nombre = txtNombre.Text.Trim(), telefono = txtTelefono.Text.Trim();
            return nuevo ? ClienteDatos.Insertar(nombre, telefono, txtEmail.Text, txtDireccion.Text)
                         : ClienteDatos.Actualizar(idActual, nombre, telefono, txtEmail.Text, txtDireccion.Text);
        }

        protected override string EliminarRegistro(int id) { return ClienteDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return Texto(fila, "Nombre"); }
    }
}
