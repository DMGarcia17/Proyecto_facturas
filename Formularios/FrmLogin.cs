using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Inicio de sesion. No hay opcion de registro: los usuarios los crea un administrador desde el menu Usuarios.
    internal class FrmLogin : Form
    {
        private const int MaxIntentos = 5;

        private readonly Validador validador = new Validador();
        private readonly CampoTexto txtUsuario, txtClave;
        private readonly BotonPildora btnIngresar;
        private readonly Panel banner;
        private readonly Label lblBanner;
        private int intentos;

        public FrmLogin()
        {
            Estilo.Formulario(this);
            Text = "Iniciar sesión";
            ClientSize = new Size(920, 580);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // Panel izquierdo: marca y ventajas
            Panel marca = new Panel { Dock = DockStyle.Fill };
            marca.Paint += PintarMarca;

            // Formulario
            Label titulo = new Label { Text = "Iniciar sesión", Font = new Font("Segoe UI Semilight", 24F), ForeColor = Estilo.Texto, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            Label subtitulo = new Label { Text = "Ingresa con tu usuario para continuar.", Font = Estilo.Normal, ForeColor = Estilo.Gris, AutoSize = true, Margin = new Padding(0, 0, 0, 22) };
            txtUsuario = new CampoTexto { Etiqueta = "Usuario", Obligatorio = true, Icono = Iconos.Usuario, Placeholder = "Tu nombre de usuario", MaxLength = 50, Ayuda = " " };
            txtClave = new CampoTexto { Etiqueta = "Contraseña", Obligatorio = true, Icono = Iconos.Candado, EsContrasena = true, Placeholder = "Tu contraseña", MaxLength = 64, Ayuda = " " };

            Interruptor mostrar = new Interruptor { Text = "Mostrar contraseña", Width = 220, Margin = new Padding(0, 0, 0, 14) };
            mostrar.CheckedChanged += (s, e) => txtClave.MostrarCaracteres(mostrar.Checked);

            banner = new Panel { Height = 48, BackColor = Estilo.ErrorClaro, Visible = false, Margin = new Padding(0, 0, 0, 12) };
            lblBanner = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Estilo.ErrorTexto, Font = Estilo.Pequena,
                Padding = new Padding(14, 0, 8, 0)
            };
            banner.Controls.Add(lblBanner);

            btnIngresar = new BotonPildora { Text = "Ingresar", Icono = Iconos.Candado, Height = 48, Margin = new Padding(0, 0, 0, 10) };
            BotonPildora btnSalir = new BotonPildora { Text = "Salir", Variante = Variante.Secundario, Height = 44, CausesValidation = false };

            Panel derecha = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(70, 70, 70, 30) };
            derecha.Controls.Add(Disposicion.Pila(5, titulo, subtitulo, txtUsuario, txtClave, mostrar, new Panel { BackColor = Color.White }, banner, btnIngresar, btnSalir));

            TableLayoutPanel raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(marca, 0, 0);
            raiz.Controls.Add(derecha, 1, 0);
            Controls.Add(raiz);

            validador.Agregar(txtUsuario, Reglas.Requerido("el usuario"))
                     .Agregar(txtClave, Reglas.Requerido("la contraseña"));

            // Aviso de Bloq Mayus mientras se escribe la contrasena
            txtClave.Caja.KeyUp += (s, e) => ActualizarMayusculas();
            txtClave.Caja.Enter += (s, e) => ActualizarMayusculas();
            txtClave.Caja.Leave += (s, e) => { txtClave.Ayuda = " "; };

            btnIngresar.Click += btnIngresar_Click;
            btnSalir.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = btnIngresar;
            CancelButton = btnSalir;
        }

        private void ActualizarMayusculas()
        {
            txtClave.Ayuda = IsKeyLocked(Keys.CapsLock) ? "Bloq Mayús está activado." : " ";
        }

        private static void PintarMarca(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush fondo = new LinearGradientBrush(p.ClientRectangle, ColorTranslator.FromHtml("#DDBFDD"), ColorTranslator.FromHtml("#B98DB9"), 62f))
                g.FillRectangle(fondo, p.ClientRectangle);

            // Formas suaves de fondo
            using (SolidBrush b = new SolidBrush(Color.FromArgb(46, Color.White))) g.FillEllipse(b, -90, p.Height - 250, 340, 340);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(38, Color.White))) g.FillEllipse(b, p.Width - 190, -100, 290, 290);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, Color.White))) g.FillEllipse(b, p.Width - 120, p.Height - 150, 90, 90);

            Color oscuro = ColorTranslator.FromHtml("#3A2440"), medio = ColorTranslator.FromHtml("#52385A");

            using (GraphicsPath chip = Estilo.Redondeado(new RectangleF(48, 56, 64, 64), 18))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(235, Color.White)))
                g.FillPath(b, chip);
            Iconos.Dibujar(g, Iconos.Factura, 30, Estilo.Primario, new RectangleF(48, 56, 64, 64));

            TextRenderer.DrawText(g, "Sistema de\nfacturación", new Font("Segoe UI Semilight", 28F), new Point(46, 146), oscuro, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Administra tus productos, clientes y ventas desde un solo lugar.", Estilo.Normal,
                new Rectangle(48, 262, 300, 60), medio, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

            string[] ventajas = { "Facturas con detalle automático", "Control de inventario en línea", "Reportes en Excel y PDF" };
            for (int i = 0; i < ventajas.Length; i++)
            {
                int y = 360 + i * 40;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(210, Color.White))) g.FillEllipse(b, 48, y, 26, 26);
                Iconos.Dibujar(g, Iconos.Check, 12, Estilo.ExitoOscuro, new RectangleF(48, y, 26, 26));
                TextRenderer.DrawText(g, ventajas[i], Estilo.Normal, new Point(84, y + 3), oscuro, TextFormatFlags.NoPadding);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtUsuario.Caja.Focus();
        }

        private void btnIngresar_Click(object sender, EventArgs ev)
        {
            banner.Visible = false;
            if (!validador.ValidarTodo()) return;

            btnIngresar.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                DataRow usuario = UsuarioDatos.Autenticar(txtUsuario.Text.Trim(), txtClave.Text);
                if (usuario == null)
                {
                    intentos++;
                    txtClave.Text = "";
                    if (intentos >= MaxIntentos)
                    {
                        Dialogo.Error(this, "Acceso bloqueado", "Demasiados intentos fallidos. La aplicación se cerrará.");
                        DialogResult = DialogResult.Cancel;
                        Close();
                        return;
                    }
                    lblBanner.Text = "Usuario o contraseña incorrectos. Intentos restantes: " + (MaxIntentos - intentos) + ".";
                    banner.Visible = true;
                    txtClave.Caja.Focus();
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
