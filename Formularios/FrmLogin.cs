using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Inicio de sesion. No hay opcion de registro: los usuarios los crea un administrador desde el menu Usuarios.
    internal class FrmLogin : Form
    {
        private const int MaxIntentos = 5;

        private readonly Validador validador = new Validador();
        private readonly TextBox txtUsuario, txtClave;
        private readonly Button btnIngresar;
        private readonly Label lblMensaje;
        private int intentos;

        public FrmLogin()
        {
            Estilo.Formulario(this);
            Text = "Iniciar sesión";
            ClientSize = new Size(440, 470);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Estilo.Encabezado(this, "Sistema de facturación", "Inicia sesión para continuar");

            Estilo.NuevaEtiqueta(this, "Usuario", 40, 120);
            txtUsuario = Estilo.NuevaCaja(this, 40, 144, 360, 50);
            Estilo.NuevaEtiqueta(this, "Contraseña", 40, 196);
            txtClave = Estilo.NuevaCaja(this, 40, 220, 360, 64);
            txtClave.UseSystemPasswordChar = true;

            CheckBox chkMostrar = new CheckBox
            {
                AutoSize = true, Text = "Mostrar contraseña", Font = Estilo.Normal, ForeColor = Estilo.Gris,
                Location = new Point(40, 256), Cursor = Cursors.Hand, TabStop = false
            };
            chkMostrar.CheckedChanged += (s, e) => txtClave.UseSystemPasswordChar = !chkMostrar.Checked;
            Controls.Add(chkMostrar);

            lblMensaje = new Label
            {
                AutoSize = false, Location = new Point(40, 290), Size = new Size(360, 40),
                ForeColor = Estilo.ErrorOscuro, Font = new Font("Segoe UI", 9.5F)
            };
            Controls.Add(lblMensaje);

            btnIngresar = Estilo.NuevoBoton(this, "Ingresar", 40, 340, 360, true);
            Button btnSalir = Estilo.NuevoBoton(this, "Salir", 40, 390, 360, false);
            btnSalir.CausesValidation = false;

            validador.Agregar(txtUsuario, Reglas.Requerido("el usuario"))
                     .Agregar(txtClave, Reglas.Requerido("la contraseña"));

            btnIngresar.Click += btnIngresar_Click;
            btnSalir.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = btnIngresar;
            CancelButton = btnSalir;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtUsuario.Focus();
        }

        private void btnIngresar_Click(object sender, EventArgs ev)
        {
            lblMensaje.Text = "";
            if (!validador.ValidarTodo()) return;

            btnIngresar.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                DataRow usuario = UsuarioDatos.Autenticar(txtUsuario.Text.Trim(), txtClave.Text);
                if (usuario == null)
                {
                    intentos++;
                    txtClave.Clear();
                    if (intentos >= MaxIntentos)
                    {
                        Estilo.Aviso(this, "Demasiados intentos fallidos. La aplicación se cerrará.", "Acceso bloqueado");
                        DialogResult = DialogResult.Cancel;
                        Close();
                        return;
                    }
                    lblMensaje.Text = "Usuario o contraseña incorrectos. Intentos restantes: " + (MaxIntentos - intentos) + ".";
                    txtClave.Focus();
                    validador.Limpiar();
                    return;
                }

                Sesion.Iniciar((int)usuario["IdUsuario"], (string)usuario["Nombre"], (string)usuario["NombreCompleto"]);
                DialogResult = DialogResult.OK;
            }
            catch (MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnIngresar.Enabled = true;
            }
        }
    }
}
