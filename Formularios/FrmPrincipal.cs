using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Contenedor MDI con menu desplegable (Productos, Clientes, Facturas, Usuarios, Ventana, Sesion).
    // Cada opcion abre su pantalla como ventana hija; si ya esta abierta, solo la trae al frente.
    internal class FrmPrincipal : Form
    {
        private readonly MenuStrip menu = new MenuStrip();
        private readonly ToolStripStatusLabel lblUsuario = new ToolStripStatusLabel();
        private readonly ToolStripStatusLabel lblReloj = new ToolStripStatusLabel();
        private readonly Timer reloj = new Timer { Interval = 15000 };
        private readonly Dictionary<string, Form> abiertas = new Dictionary<string, Form>();
        private MdiClient cliente;
        private bool cerrandoSesion;

        // True si el usuario pidio cerrar sesion (Program vuelve a mostrar el login)
        public bool CerrarSesionSolicitada { get { return cerrandoSesion; } }

        public FrmPrincipal()
        {
            Estilo.Formulario(this);
            IsMdiContainer = true;
            Text = "Sistema de facturación";
            ClientSize = new Size(1200, 760);
            MinimumSize = new Size(1000, 700);
            WindowState = FormWindowState.Maximized;

            ConstruirMenu();
            ConstruirBarraEstado();
            ConstruirBienvenida();

            MdiChildActivate += (s, e) => ActualizarBienvenida();
            FormClosing += FrmPrincipal_FormClosing;
            FormClosed += (s, e) => reloj.Dispose();
        }

        // ---------- Menu ----------
        private void ConstruirMenu()
        {
            menu.Renderer = new MenuRenderer();
            menu.BackColor = Estilo.Primario;
            menu.ForeColor = Color.White;
            menu.Font = new Font("Segoe UI", 10F);
            menu.Padding = new Padding(8, 4, 0, 4);

            menu.Items.Add(Raiz("&Productos",
                Opcion("&Nuevo producto", () => Abrir("prod-nuevo", () => new FrmProductos(Modo.Nuevo))),
                Opcion("&Consultar productos", () => Abrir("prod-consultar", () => new FrmProductos(Modo.Consultar))),
                Opcion("&Editar producto", () => Abrir("prod-editar", () => new FrmProductos(Modo.Editar))),
                Opcion("E&liminar producto", () => Abrir("prod-eliminar", () => new FrmProductos(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Clientes",
                Opcion("&Nuevo cliente", () => Abrir("cli-nuevo", () => new FrmClientes(Modo.Nuevo))),
                Opcion("&Consultar clientes", () => Abrir("cli-consultar", () => new FrmClientes(Modo.Consultar))),
                Opcion("&Editar cliente", () => Abrir("cli-editar", () => new FrmClientes(Modo.Editar))),
                Opcion("E&liminar cliente", () => Abrir("cli-eliminar", () => new FrmClientes(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Facturas",
                Opcion("&Nueva factura", () => Abrir("fac-nueva", () => new FrmFacturaNueva()), Keys.Control | Keys.N),
                Opcion("&Consultar facturas", () => Abrir("fac-consultar", () => new FrmFacturas(Modo.Consultar))),
                Opcion("E&liminar factura", () => Abrir("fac-eliminar", () => new FrmFacturas(Modo.Eliminar))),
                new ToolStripSeparator(),
                Opcion("&Reporte de ventas…", () => Abrir("fac-reporte", () => new FrmReporteVentas()), Keys.Control | Keys.R)));

            menu.Items.Add(Raiz("&Usuarios",
                Opcion("&Nuevo usuario", () => Abrir("usu-nuevo", () => new FrmUsuarios(Modo.Nuevo))),
                Opcion("&Consultar usuarios", () => Abrir("usu-consultar", () => new FrmUsuarios(Modo.Consultar))),
                Opcion("&Editar usuario", () => Abrir("usu-editar", () => new FrmUsuarios(Modo.Editar))),
                Opcion("E&liminar usuario", () => Abrir("usu-eliminar", () => new FrmUsuarios(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Ventana",
                Opcion("&Cascada", () => LayoutMdi(MdiLayout.Cascade)),
                Opcion("Mosaico &horizontal", () => LayoutMdi(MdiLayout.TileHorizontal)),
                Opcion("Mosaico &vertical", () => LayoutMdi(MdiLayout.TileVertical)),
                new ToolStripSeparator(),
                Opcion("Cerrar &todas", CerrarTodas)));

            ToolStripMenuItem sesion = Raiz("&Sesión",
                Opcion("&Cerrar sesión", CerrarSesion, Keys.Control | Keys.L),
                new ToolStripSeparator(),
                Opcion("&Salir", Close, Keys.None, "Alt+F4"));
            sesion.Alignment = ToolStripItemAlignment.Right;
            menu.Items.Add(sesion);

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private static ToolStripMenuItem Raiz(string texto, params ToolStripItem[] hijos)
        {
            ToolStripMenuItem raiz = new ToolStripMenuItem(texto) { ForeColor = Color.White, Padding = new Padding(10, 2, 10, 2) };
            raiz.DropDownItems.AddRange(hijos);
            return raiz;
        }

        private static ToolStripMenuItem Opcion(string texto, Action accion, Keys atajo = Keys.None, string textoAtajo = null)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(texto)
            {
                ForeColor = Estilo.Texto, BackColor = Color.White, Padding = new Padding(2, 5, 2, 5)
            };
            if (atajo != Keys.None) item.ShortcutKeys = atajo;
            if (textoAtajo != null) item.ShortcutKeyDisplayString = textoAtajo;
            item.Click += (s, e) => accion();
            return item;
        }

        // ---------- Barra de estado ----------
        private void ConstruirBarraEstado()
        {
            StatusStrip barra = new StatusStrip { BackColor = Color.White, SizingGrip = false };
            lblUsuario.Text = "Usuario: " + Sesion.NombreCompleto + " (" + Sesion.Nombre + ")";
            lblUsuario.ForeColor = Estilo.Texto;
            ToolStripStatusLabel lblBd = new ToolStripStatusLabel
            {
                Text = "Base de datos: " + Conexion.NombreBD, ForeColor = Estilo.Gris, Spring = true, TextAlign = ContentAlignment.MiddleRight
            };
            lblReloj.ForeColor = Estilo.Gris;
            barra.Items.AddRange(new ToolStripItem[] { lblUsuario, lblBd, lblReloj });
            Controls.Add(barra);

            ActualizarReloj();
            reloj.Tick += (s, e) => ActualizarReloj();
            reloj.Start();
        }

        private void ActualizarReloj()
        {
            lblReloj.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
        }

        // ---------- Fondo de bienvenida ----------
        private void ConstruirBienvenida()
        {
            // El area MDI no admite controles, asi que el mensaje se dibuja directamente sobre ella
            cliente = Controls.OfType<MdiClient>().FirstOrDefault();
            if (cliente == null) return;
            cliente.BackColor = Estilo.Fondo;
            cliente.Paint += (s, e) =>
            {
                if (MdiChildren.Length > 0) return;
                using (Font titulo = new Font("Segoe UI Semibold", 22F))
                using (Font texto = new Font("Segoe UI", 11F))
                using (SolidBrush oscuro = new SolidBrush(Estilo.Texto))
                using (SolidBrush gris = new SolidBrush(Estilo.Gris))
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString("Bienvenido, " + Sesion.NombreCompleto, titulo, oscuro, 48, 48);
                    e.Graphics.DrawString(
                        "Usa el menú superior para administrar productos, clientes, facturas y usuarios.\n" +
                        "Atajos: Ctrl+N nueva factura  ·  Ctrl+R reporte de ventas  ·  Ctrl+L cerrar sesión",
                        texto, gris, 52, 100);
                }
            };
            cliente.Resize += (s, e) => cliente.Invalidate();
        }

        private void ActualizarBienvenida()
        {
            if (cliente != null) cliente.Invalidate();
        }

        // ---------- Ventanas hijas ----------
        private void Abrir(string clave, Func<Form> crear)
        {
            if (abiertas.TryGetValue(clave, out Form existente) && !existente.IsDisposed)
            {
                if (existente.WindowState == FormWindowState.Minimized) existente.WindowState = FormWindowState.Normal;
                existente.Activate();
                return;
            }

            Form hijo = crear();
            hijo.MdiParent = this;
            abiertas[clave] = hijo;
            hijo.FormClosed += (s, e) =>
            {
                abiertas.Remove(clave);
                BeginInvoke(new Action(ActualizarBienvenida));
            };
            hijo.Show();
            ActualizarBienvenida();
        }

        private void CerrarTodas()
        {
            foreach (Form hijo in MdiChildren.ToArray()) hijo.Close();
        }

        // ---------- Sesion ----------
        private void CerrarSesion()
        {
            if (!Estilo.Confirmar(this, "¿Quieres cerrar la sesión de " + Sesion.Nombre + "?", "Cerrar sesión")) return;
            cerrandoSesion = true;
            Sesion.Cerrar();
            Close();
        }

        private void FrmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cerrandoSesion || e.CloseReason != CloseReason.UserClosing) return;
            if (!Estilo.Confirmar(this, "¿Seguro que quieres salir de la aplicación?", "Salir")) e.Cancel = true;
        }

        // ---------- Aspecto del menu ----------
        private class MenuRenderer : ToolStripProfessionalRenderer
        {
            public MenuRenderer() : base(new Colores()) { RoundedEdges = false; }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle area = new Rectangle(Point.Empty, e.Item.Size);
                Color color = e.Item.IsOnDropDown ? Estilo.PrimarioClaro : Estilo.PrimarioOscuro;
                if (e.Item.Selected || e.Item.Pressed)
                    using (SolidBrush pincel = new SolidBrush(color))
                        e.Graphics.FillRectangle(pincel, area);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (e.Item.IsOnDropDown)
                    e.TextColor = e.Item.Enabled ? Estilo.Texto : Estilo.Gris;
                else
                    e.TextColor = Color.White;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                using (SolidBrush pincel = new SolidBrush(Color.White))
                    e.Graphics.FillRectangle(pincel, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is ToolStripDropDown)
                    using (Pen lapiz = new Pen(Color.FromArgb(209, 213, 219)))
                        e.Graphics.DrawRectangle(lapiz, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
            }
        }

        private class Colores : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Color.White; } }
            public override Color ImageMarginGradientBegin { get { return Color.White; } }
            public override Color ImageMarginGradientMiddle { get { return Color.White; } }
            public override Color ImageMarginGradientEnd { get { return Color.White; } }
            public override Color MenuStripGradientBegin { get { return Estilo.Primario; } }
            public override Color MenuStripGradientEnd { get { return Estilo.Primario; } }
            public override Color SeparatorDark { get { return Color.FromArgb(229, 231, 235); } }
            public override Color SeparatorLight { get { return Color.White; } }
        }
    }
}
