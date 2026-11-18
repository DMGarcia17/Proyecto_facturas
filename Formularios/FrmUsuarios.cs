using System.Data;
using System.Windows.Forms;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de usuarios del sistema (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmUsuarios : FrmCrudBase
    {
        private TextBox txtUsuario, txtNombreCompleto, txtClave, txtConfirmar;

        public FrmUsuarios(Modo modo) : base(modo, "usuario", "usuarios") { }

        protected override string ColumnaId { get { return "IdUsuario"; } }

        protected override int CrearCampos(int y)
        {
            bool editar = modo == Modo.Editar;
            txtUsuario = Campo("Usuario", 24, y, 260, 50);
            txtNombreCompleto = Campo("Nombre completo", 304, y, 612, 150);
            txtClave = Campo(editar ? "Nueva contraseña (opcional)" : "Contraseña", 24, y + 66, 260, 64);
            txtConfirmar = Campo("Confirmar contraseña", 304, y + 66, 260, 64);
            txtClave.UseSystemPasswordChar = txtConfirmar.UseSystemPasswordChar = true;

            Estilo.NuevaEtiqueta(this, "Mínimo 8 caracteres, con letras y números", 584, y + 96).ForeColor = Estilo.Gris;

            validador.Agregar(txtUsuario, Reglas.Usuario())
                     .Agregar(txtNombreCompleto, Reglas.Texto("el nombre completo", 3, 150))
                     .Agregar(txtClave, editar ? Reglas.Opcional(Reglas.Contrasena()) : Reglas.Contrasena())
                     .Agregar(txtConfirmar, Reglas.Coincide(() => txtClave.Text, "Las contraseñas no coinciden."));
            txtClave.TextChanged += (s, e) => { if (txtConfirmar.Text.Length > 0) validador.Validar(txtConfirmar); };
            return 132;
        }

        protected override DataTable Listar(string filtro) { return UsuarioDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "IdUsuario", "Id", 25, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "Nombre", "Usuario", 60, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "NombreCompleto", "Nombre completo", 110, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "FechaRegistro", "Fecha de registro", 70, "dd/MM/yyyy HH:mm", Estilo.Izquierda);
        }

        protected override void CargarCampos(DataGridViewRow fila)
        {
            txtUsuario.Text = (string)fila.Cells["Nombre"].Value;
            txtNombreCompleto.Text = (string)fila.Cells["NombreCompleto"].Value;
            txtClave.Clear();
            txtConfirmar.Clear();
        }

        protected override void LimpiarCampos()
        {
            txtUsuario.Clear(); txtNombreCompleto.Clear(); txtClave.Clear(); txtConfirmar.Clear();
            if (txtUsuario.Enabled) txtUsuario.Focus();
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtUsuario.Enabled = txtNombreCompleto.Enabled = txtClave.Enabled = txtConfirmar.Enabled = habilitado;
        }

        protected override string Guardar(bool nuevo)
        {
            string usuario = txtUsuario.Text.Trim(), completo = txtNombreCompleto.Text.Trim();
            return nuevo ? UsuarioDatos.Insertar(usuario, completo, txtClave.Text)
                         : UsuarioDatos.Actualizar(idActual, usuario, completo, txtClave.Text);
        }

        protected override string EliminarRegistro(int id) { return UsuarioDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return (string)fila.Cells["Nombre"].Value; }
    }
}
