using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    // Contenedor MDI con menu desplegable (Productos, Clientes, Facturas, Usuarios, Ventana, Sesion) y accesos rapidos.
    // Cada opcion abre su pantalla como ventana hija; si ya esta abierta, solo la trae al frente.
    internal class FrmPrincipal : Form
    {
        private readonly MenuStrip menu = new MenuStrip();
        private readonly ToolStripStatusLabel lblReloj = new ToolStripStatusLabel();
        private readonly Timer reloj = new Timer { Interval = 15000 };
        private readonly Dictionary<string, Form> abiertas = new Dictionary<string, Form>();
        private MdiClient cliente;
        private bool cerrandoSesion;
        private string cifraProductos = "-", cifraClientes = "-", cifraFacturasHoy = "-";

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

            Controls.Add(ConstruirBarraEstado());
            Controls.Add(ConstruirAccesosRapidos());
            ConstruirMenu();
            ConstruirBienvenida();

            MdiChildActivate += (s, e) => cliente?.Invalidate();
            FormClosing += FrmPrincipal_FormClosing;
            FormClosed += (s, e) => reloj.Dispose();
        }

        // ---------- Menu desplegable ----------
        private void ConstruirMenu()
        {
            menu.Renderer = new RenderizadorSuave();
            menu.BackColor = Color.White;
            menu.Font = new Font("Segoe UI", 10F);
            menu.Padding = new Padding(10, 6, 10, 6);
            menu.ImageScalingSize = new Size(18, 18);

            menu.Items.Add(Raiz("&Productos",
                Opcion("&Nuevo producto", Iconos.Agregar, () => Abrir("prod-nuevo", () => new FrmProductos(Modo.Nuevo))),
                Opcion("&Consultar productos", Iconos.Buscar, () => Abrir("prod-consultar", () => new FrmProductos(Modo.Consultar))),
                Opcion("&Editar producto", Iconos.Editar, () => Abrir("prod-editar", () => new FrmProductos(Modo.Editar))),
                Opcion("E&liminar producto", Iconos.Eliminar, () => Abrir("prod-eliminar", () => new FrmProductos(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Clientes",
                Opcion("&Nuevo cliente", Iconos.Agregar, () => Abrir("cli-nuevo", () => new FrmClientes(Modo.Nuevo))),
                Opcion("&Consultar clientes", Iconos.Buscar, () => Abrir("cli-consultar", () => new FrmClientes(Modo.Consultar))),
                Opcion("&Editar cliente", Iconos.Editar, () => Abrir("cli-editar", () => new FrmClientes(Modo.Editar))),
                Opcion("E&liminar cliente", Iconos.Eliminar, () => Abrir("cli-eliminar", () => new FrmClientes(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Facturas",
                Opcion("&Nueva factura", Iconos.Agregar, () => Abrir("fac-nueva", () => new FrmFacturaNueva()), Keys.Control | Keys.N),
                Opcion("&Consultar facturas", Iconos.Buscar, () => Abrir("fac-consultar", () => new FrmFacturas(Modo.Consultar))),
                Opcion("E&liminar factura", Iconos.Eliminar, () => Abrir("fac-eliminar", () => new FrmFacturas(Modo.Eliminar))),
                new ToolStripSeparator(),
                Opcion("&Reporte de ventas…", Iconos.Reporte, () => Abrir("fac-reporte", () => new FrmReporteVentas()), Keys.Control | Keys.R)));

            menu.Items.Add(Raiz("&Usuarios",
                Opcion("&Nuevo usuario", Iconos.Agregar, () => Abrir("usu-nuevo", () => new FrmUsuarios(Modo.Nuevo))),
                Opcion("&Consultar usuarios", Iconos.Buscar, () => Abrir("usu-consultar", () => new FrmUsuarios(Modo.Consultar))),
                Opcion("&Editar usuario", Iconos.Editar, () => Abrir("usu-editar", () => new FrmUsuarios(Modo.Editar))),
                Opcion("E&liminar usuario", Iconos.Eliminar, () => Abrir("usu-eliminar", () => new FrmUsuarios(Modo.Eliminar)))));

            menu.Items.Add(Raiz("&Ventana",
                Opcion("&Cascada", Iconos.Etiqueta, () => LayoutMdi(MdiLayout.Cascade)),
                Opcion("Mosaico &horizontal", Iconos.Etiqueta, () => LayoutMdi(MdiLayout.TileHorizontal)),
                Opcion("Mosaico &vertical", Iconos.Etiqueta, () => LayoutMdi(MdiLayout.TileVertical)),
                new ToolStripSeparator(),
                Opcion("Cerrar &todas", Iconos.Cerrar, CerrarTodas)));

            ToolStripMenuItem sesion = Raiz("&Sesión",
                Opcion("&Cerrar sesión", Iconos.Usuario, CerrarSesion, Keys.Control | Keys.L),
                new ToolStripSeparator(),
                Opcion("&Salir", Iconos.Salir, Close, Keys.None, "Alt+F4"));
            sesion.Alignment = ToolStripItemAlignment.Right;
            menu.Items.Add(sesion);

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private static ToolStripMenuItem Raiz(string texto, params ToolStripItem[] hijos)
        {
            ToolStripMenuItem raiz = new ToolStripMenuItem(texto) { ForeColor = Estilo.Texto, Padding = new Padding(12, 3, 12, 3) };
            raiz.DropDownItems.AddRange(hijos);
            return raiz;
        }

        private static ToolStripMenuItem Opcion(string texto, string icono, Action accion, Keys atajo = Keys.None, string textoAtajo = null)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(texto)
            {
                ForeColor = Estilo.Texto, BackColor = Color.White, Padding = new Padding(2, 6, 2, 6),
                Image = Iconos.Imagen(icono, 18, Estilo.Primario)
            };
            if (atajo != Keys.None) item.ShortcutKeys = atajo;
            if (textoAtajo != null) item.ShortcutKeyDisplayString = textoAtajo;
            item.Click += (s, e) => accion();
            return item;
        }

        // ---------- Accesos rapidos ----------
        private Control ConstruirAccesosRapidos()
        {
            FlowLayoutPanel barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 62, BackColor = Estilo.Fondo, Padding = new Padding(18, 11, 18, 0), WrapContents = false
            };
            barra.Controls.Add(Rapido("Nueva factura", Iconos.Agregar, Variante.Primario, 168, () => Abrir("fac-nueva", () => new FrmFacturaNueva())));
            barra.Controls.Add(Rapido("Facturas", Iconos.Factura, Variante.Secundario, 130, () => Abrir("fac-consultar", () => new FrmFacturas(Modo.Consultar))));
            barra.Controls.Add(Rapido("Clientes", Iconos.Clientes, Variante.Secundario, 130, () => Abrir("cli-consultar", () => new FrmClientes(Modo.Consultar))));
            barra.Controls.Add(Rapido("Productos", Iconos.Productos, Variante.Secundario, 136, () => Abrir("prod-consultar", () => new FrmProductos(Modo.Consultar))));
            barra.Controls.Add(Rapido("Reporte de ventas", Iconos.Reporte, Variante.Secundario, 176, () => Abrir("fac-reporte", () => new FrmReporteVentas())));
            return barra;
        }

        private static BotonPildora Rapido(string texto, string icono, Variante variante, int ancho, Action accion)
        {
            BotonPildora b = new BotonPildora { Text = texto, Icono = icono, Variante = variante, Size = new Size(ancho, 38), Margin = new Padding(0, 0, 10, 0) };
            b.Click += (s, e) => accion();
            return b;
        }

        // ---------- Barra de estado ----------
        private Control ConstruirBarraEstado()
        {
            StatusStrip barra = new StatusStrip { BackColor = Color.White, SizingGrip = false, Padding = new Padding(10, 4, 10, 4) };
            ToolStripStatusLabel lblUsuario = new ToolStripStatusLabel
            {
                Text = "Usuario: " + Sesion.NombreCompleto + " (" + Sesion.Nombre + ")", ForeColor = Estilo.Texto,
                Image = Iconos.Imagen(Iconos.Usuario, 16, Estilo.Primario), ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText
            };
            ToolStripStatusLabel lblBd = new ToolStripStatusLabel
            {
                Text = "Base de datos: " + Conexion.NombreBD, ForeColor = Estilo.Gris, Spring = true, TextAlign = ContentAlignment.MiddleRight
            };
            lblReloj.ForeColor = Estilo.Gris;
            barra.Items.AddRange(new ToolStripItem[] { lblUsuario, lblBd, lblReloj });

            ActualizarReloj();
            reloj.Tick += (s, e) => ActualizarReloj();
            reloj.Start();
            return barra;
        }

        private void ActualizarReloj()
        {
            lblReloj.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
        }

        // ---------- Fondo de bienvenida (el area MDI no admite controles, se pinta) ----------
        private void ConstruirBienvenida()
        {
            cliente = Controls.OfType<MdiClient>().FirstOrDefault();
            if (cliente == null) return;
            cliente.BackColor = Estilo.Fondo;
            QuitarBordeMdi(cliente);
            cliente.Paint += PintarBienvenida;
            cliente.Resize += (s, e) => cliente.Invalidate();
        }

        // El area MDI trae un borde hundido del sistema; se quita para que quede plana como el resto
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int indice);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int indice, int valor);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr despues, int x, int y, int ancho, int alto, uint flags);

        private static void QuitarBordeMdi(MdiClient mdi)
        {
            const int GWL_EXSTYLE = -20, WS_EX_CLIENTEDGE = 0x200;
            const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_FRAMECHANGED = 0x20;
            int estilo = GetWindowLong(mdi.Handle, GWL_EXSTYLE);
            SetWindowLong(mdi.Handle, GWL_EXSTYLE, estilo & ~WS_EX_CLIENTEDGE);
            SetWindowPos(mdi.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private void PintarBienvenida(object sender, PaintEventArgs e)
        {
            if (MdiChildren.Length > 0) return;
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            TextRenderer.DrawText(g, "Bienvenido, " + Sesion.NombreCompleto, new Font("Segoe UI Semilight", 26F), new Point(44, 40), Estilo.Texto, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Usa el menú superior o los accesos rápidos para administrar productos, clientes, facturas y usuarios.",
                Estilo.Normal, new Point(48, 94), Estilo.Gris, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, "Atajos:  Ctrl+N nueva factura   ·   Ctrl+R reporte de ventas   ·   Ctrl+L cerrar sesión",
                Estilo.Pequena, new Point(48, 122), Estilo.Gris, TextFormatFlags.NoPadding);

            int x = 44, y = 170, ancho = 270;
            TarjetaResumen.Dibujar(g, new Rectangle(x, y, ancho, 96), Iconos.Productos, "Productos registrados", cifraProductos, Estilo.InfoClaro, Estilo.InfoTexto);
            TarjetaResumen.Dibujar(g, new Rectangle(x + ancho + 16, y, ancho, 96), Iconos.Clientes, "Clientes registrados", cifraClientes, Estilo.ExitoClaro, Estilo.ExitoOscuro);
            TarjetaResumen.Dibujar(g, new Rectangle(x + 2 * (ancho + 16), y, ancho, 96), Iconos.Factura, "Facturas emitidas hoy", cifraFacturasHoy, Estilo.AvisoClaro, Estilo.AvisoTexto);
        }

        // Cifras de la pantalla de bienvenida (se consultan sin bloquear la ventana)
        private void CargarResumen()
        {
            Task.Run(() =>
            {
                try
                {
                    string productos = Convert.ToInt64(Bd.Escalar("SELECT COUNT(*) FROM Producto")).ToString("N0");
                    string clientes = Convert.ToInt64(Bd.Escalar("SELECT COUNT(*) FROM Cliente")).ToString("N0");
                    string hoy = Convert.ToInt64(Bd.Escalar("SELECT COUNT(*) FROM Factura WHERE Fecha >= @h",
                        Bd.P("@h", DateTime.Today))).ToString("N0");
                    if (!IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action(() =>
                        {
                            cifraProductos = productos; cifraClientes = clientes; cifraFacturasHoy = hoy;
                            cliente?.Invalidate();
                        }));
                }
                catch (Exception)
                {
                    // Sin conexion: se dejan los guiones
                }
            });
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CargarResumen();
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

            // Ajusta la ventana al area disponible y la centra
            Rectangle area = cliente != null ? cliente.ClientRectangle : ClientRectangle;
            int ancho = Math.Min(hijo.Width, Math.Max(hijo.MinimumSize.Width, area.Width - 32));
            int alto = Math.Min(hijo.Height, Math.Max(hijo.MinimumSize.Height, area.Height - 32));
            hijo.StartPosition = FormStartPosition.Manual;
            hijo.Size = new Size(ancho, alto);
            hijo.Location = new Point(Math.Max(0, (area.Width - ancho) / 2), Math.Max(0, (area.Height - alto) / 2));

            abiertas[clave] = hijo;
            hijo.FormClosed += (s, e) =>
            {
                abiertas.Remove(clave);
                if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => { cliente?.Invalidate(); CargarResumen(); }));
            };
            hijo.Show();
            cliente?.Invalidate();
        }

        private void CerrarTodas()
        {
            foreach (Form hijo in MdiChildren.ToArray()) hijo.Close();
        }

        // ---------- Sesion ----------
        private void CerrarSesion()
        {
            if (!Estilo.Confirmar(this, "Volverás a la pantalla de inicio de sesión. Las ventanas abiertas se cerrarán.", "¿Cerrar la sesión de " + Sesion.Nombre + "?", "Sí, cerrar sesión")) return;
            cerrandoSesion = true;
            Sesion.Cerrar();
            Close();
        }

        private void FrmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cerrandoSesion || e.CloseReason != CloseReason.UserClosing) return;
            if (!Estilo.Confirmar(this, "Se cerrarán todas las ventanas abiertas.", "¿Salir de la aplicación?", "Sí, salir")) e.Cancel = true;
        }

        // ---------- Aspecto del menu ----------
        private class RenderizadorSuave : ToolStripProfessionalRenderer
        {
            public RenderizadorSuave() : base(new Colores()) { RoundedEdges = false; }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.White)) e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected && !e.Item.Pressed) return;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF area = e.Item.IsOnDropDown
                    ? new RectangleF(4, 1, e.Item.Width - 8, e.Item.Height - 2)
                    : new RectangleF(1, 1, e.Item.Width - 3, e.Item.Height - 2);
                using (GraphicsPath ruta = Estilo.Redondeado(area, e.Item.IsOnDropDown ? 8 : 10))
                using (SolidBrush b = new SolidBrush(Estilo.PrimarioClaro))
                    g.FillPath(b, ruta);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Estilo.Texto : Estilo.Gris;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.White)) e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using (Pen p = new Pen(Estilo.Borde))
                    e.Graphics.DrawLine(p, 12, e.Item.Height / 2, e.Item.Width - 12, e.Item.Height / 2);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (Pen p = new Pen(Estilo.Borde))
                {
                    if (e.ToolStrip is ToolStripDropDown)
                        e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                    else
                        e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
                }
            }
        }

        private class Colores : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Color.White; } }
            public override Color ImageMarginGradientBegin { get { return Color.White; } }
            public override Color ImageMarginGradientMiddle { get { return Color.White; } }
            public override Color ImageMarginGradientEnd { get { return Color.White; } }
            public override Color MenuStripGradientBegin { get { return Color.White; } }
            public override Color MenuStripGradientEnd { get { return Color.White; } }
            public override Color MenuBorder { get { return Estilo.Borde; } }
        }
    }
}
