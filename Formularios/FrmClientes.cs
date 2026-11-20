using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de clientes (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmClientes : FrmCrudBase
    {
        private CampoTexto txtNombre, txtTelefono, txtEmail, txtDireccion;

        public FrmClientes(Modo modo) : base(modo, "cliente", "clientes") { }

        protected override string ColumnaId { get { return "Idcliente"; } }
        protected override string IconoEntidad { get { return Iconos.Clientes; } }

        protected override Control CrearFormulario()
        {
            txtNombre = new CampoTexto
            {
                Etiqueta = "Nombre o razón social", Obligatorio = true, Placeholder = "Ej. María López",
                Ayuda = "Entre 3 y 100 caracteres.", Icono = Iconos.Usuario, MaxLength = 100
            };
            txtTelefono = new CampoTexto
            {
                Etiqueta = "Teléfono", Obligatorio = true, Placeholder = "2222-3344",
                Ayuda = "De 8 a 15 dígitos.", Icono = Iconos.Telefono, MaxLength = 20
            };
            txtEmail = new CampoTexto
            {
                Etiqueta = "Correo electrónico", Placeholder = "nombre@dominio.com",
                Ayuda = "Opcional. Debe ser único.", Icono = Iconos.Correo, MaxLength = 150
            };
            txtDireccion = new CampoTexto
            {
                Etiqueta = "Dirección", Placeholder = "Calle, colonia, ciudad",
                Ayuda = "Opcional.", Icono = Iconos.Ubicacion, MaxLength = 255
            };

            Validador.Filtrar(txtTelefono, Filtro.Telefono);
            // Si el telefono son solo 8 digitos, se muestra como 2222-3344 al salir del campo
            txtTelefono.Caja.Validated += (s, e) =>
            {
                string t = txtTelefono.Text.Trim();
                if (t.Length == 8 && t.All(char.IsDigit)) txtTelefono.Text = t.Substring(0, 4) + "-" + t.Substring(4);
            };
            validador.Agregar(txtNombre, Reglas.Texto("el nombre", 3, 100))
                     .Agregar(txtTelefono, Reglas.Telefono())
                     .Agregar(txtEmail, Reglas.Opcional(Reglas.Email()))
                     .Agregar(txtDireccion, Reglas.Opcional(Reglas.Texto("la dirección", 5, 255, false)));

            return Apilar(
                new SeccionForm("Datos generales", "Cómo se identificará al cliente y cómo contactarlo.")
                    .Fila(txtNombre).Fila(txtTelefono, txtEmail),
                new SeccionForm("Ubicación", "Dirección de facturación o entrega.").Fila(txtDireccion));
        }

        protected override DataTable Listar(string filtro) { return ClienteDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "Idcliente", "Id", 20, null, Estilo.Izquierda, 40);
            Estilo.Columna(dgv, "Nombre", "Nombre", 90, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Telefono", "Teléfono", 55, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Email", "Correo", 80, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Direccion", "Dirección", 80, null, Estilo.Izquierda);

            // En las vistas con formulario al lado la lista es angosta: solo se muestran las columnas principales
            if (modo != Modo.Consultar)
            {
                dgv.Columns["Email"].Visible = false;
                dgv.Columns["Direccion"].Visible = false;
            }
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
            txtNombre.Text = txtTelefono.Text = txtEmail.Text = txtDireccion.Text = "";
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtNombre.Enabled = txtTelefono.Enabled = txtEmail.Enabled = txtDireccion.Enabled = habilitado;
        }

        protected override void EnfocarPrimero() { if (txtNombre.Enabled) txtNombre.Caja.Focus(); }

        protected override string Guardar(bool nuevo)
        {
            string nombre = txtNombre.Text.Trim(), telefono = txtTelefono.Text.Trim();
            return nuevo ? ClienteDatos.Insertar(nombre, telefono, txtEmail.Text, txtDireccion.Text)
                         : ClienteDatos.Actualizar(idActual, nombre, telefono, txtEmail.Text, txtDireccion.Text);
        }

        protected override bool MarcarErrorServidor(string error)
        {
            if (!error.Contains("nombre o correo")) return false;
            validador.Marcar(txtNombre, "Ya existe un cliente con este nombre o correo.");
            if (txtEmail.Text.Trim().Length > 0) validador.Marcar(txtEmail, "Revisa que el correo no esté repetido.");
            return true;
        }

        protected override string EliminarRegistro(int id) { return ClienteDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return Texto(fila, "Nombre"); }
    }
}
