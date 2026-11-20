using System.Data;
using System.Windows.Forms;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // CRUD de usuarios del sistema (Nuevo / Consultar / Editar / Eliminar segun el modo)
    internal class FrmUsuarios : FrmCrudBase
    {
        private CampoTexto txtUsuario, txtNombreCompleto, txtClave, txtConfirmar;
        private MedidorClave medidor;

        public FrmUsuarios(Modo modo) : base(modo, "usuario", "usuarios") { }

        protected override string ColumnaId { get { return "IdUsuario"; } }
        protected override string IconoEntidad { get { return Iconos.Usuario; } }

        protected override Control CrearFormulario()
        {
            bool editar = modo == Modo.Editar;
            txtUsuario = new CampoTexto
            {
                Etiqueta = "Usuario", Obligatorio = true, Placeholder = "Ej. mlopez", Icono = Iconos.Usuario,
                Ayuda = "3 a 50 caracteres: letras, números, . - _", MaxLength = 50
            };
            txtNombreCompleto = new CampoTexto
            {
                Etiqueta = "Nombre completo", Obligatorio = true, Placeholder = "Ej. María López",
                Ayuda = "Como aparecerá en la barra de estado.", MaxLength = 150
            };
            txtClave = new CampoTexto
            {
                Etiqueta = editar ? "Nueva contraseña" : "Contraseña", Obligatorio = !editar, EsContrasena = true, OjoVisible = true,
                Icono = Iconos.Candado, Placeholder = editar ? "Déjala vacía para conservar la actual" : "Crea una contraseña", MaxLength = 64,
                Ayuda = editar ? "Opcional al editar." : "Entre 8 y 64 caracteres."
            };
            txtConfirmar = new CampoTexto
            {
                Etiqueta = "Confirmar contraseña", Obligatorio = !editar, EsContrasena = true, OjoVisible = true,
                Icono = Iconos.Candado, Placeholder = "Repítela", MaxLength = 64, Ayuda = "Debe coincidir con la anterior."
            };
            medidor = new MedidorClave();
            txtClave.Caja.TextChanged += (s, e) =>
            {
                medidor.Evaluar(txtClave.Text);
                if (txtConfirmar.Text.Length > 0) validador.Validar(txtConfirmar);
            };

            validador.Agregar(txtUsuario, Reglas.Usuario())
                     .Agregar(txtNombreCompleto, Reglas.Texto("el nombre completo", 3, 150))
                     .Agregar(txtClave, editar ? Reglas.Opcional(Reglas.Contrasena()) : Reglas.Contrasena())
                     .Agregar(txtConfirmar, Reglas.Coincide(() => txtClave.Text, "Las contraseñas no coinciden."));

            return Apilar(
                new SeccionForm("Datos de la cuenta", "Quién es y con qué nombre inicia sesión.").Fila(txtUsuario, txtNombreCompleto),
                new SeccionForm("Contraseña", editar ? "Solo escribe una nueva si quieres cambiarla." : "Usa una contraseña segura y fácil de recordar.")
                    .Fila(txtClave, txtConfirmar).Fila(new float[] { 1 }, medidor));
        }

        protected override DataTable Listar(string filtro) { return UsuarioDatos.Listar(filtro); }

        protected override void FormatoGrilla()
        {
            Estilo.Columna(dgv, "IdUsuario", "Id", 20, null, Estilo.Izquierda, 40);
            Estilo.Columna(dgv, "Nombre", "Usuario", 55, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "NombreCompleto", "Nombre completo", 100, null, Estilo.Izquierda);
            Estilo.Columna(dgv, "FechaRegistro", "Registro", 65, "dd/MM/yyyy", Estilo.Izquierda, 100);
        }

        protected override void CargarCampos(DataGridViewRow fila)
        {
            txtUsuario.Text = (string)fila.Cells["Nombre"].Value;
            txtNombreCompleto.Text = (string)fila.Cells["NombreCompleto"].Value;
            txtClave.Text = txtConfirmar.Text = "";
        }

        protected override void LimpiarCampos()
        {
            txtUsuario.Text = txtNombreCompleto.Text = txtClave.Text = txtConfirmar.Text = "";
            medidor.Evaluar("");
        }

        protected override void HabilitarCampos(bool habilitado)
        {
            txtUsuario.Enabled = txtNombreCompleto.Enabled = txtClave.Enabled = txtConfirmar.Enabled = habilitado;
        }

        protected override void EnfocarPrimero() { if (txtUsuario.Enabled) txtUsuario.Caja.Focus(); }

        protected override string Guardar(bool nuevo)
        {
            string usuario = txtUsuario.Text.Trim(), completo = txtNombreCompleto.Text.Trim();
            return nuevo ? UsuarioDatos.Insertar(usuario, completo, txtClave.Text)
                         : UsuarioDatos.Actualizar(idActual, usuario, completo, txtClave.Text);
        }

        protected override bool MarcarErrorServidor(string error)
        {
            if (!error.Contains("nombre de usuario")) return false;
            validador.Marcar(txtUsuario, "Este nombre de usuario ya está en uso.");
            return true;
        }

        protected override string EliminarRegistro(int id) { return UsuarioDatos.Eliminar(id); }

        protected override string Resumen(DataGridViewRow fila) { return (string)fila.Cells["Nombre"].Value; }
    }
}
