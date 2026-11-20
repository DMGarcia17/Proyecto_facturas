using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Controles;

namespace Proyecto_facturas.Utilidades
{
    // Paleta, tipografia y ayudas visuales compartidas. Estilo suave: tarjetas blancas redondeadas,
    // botones tipo pildora y tonos pastel con el lila #C8A2C8 como color de marca.
    internal static class Estilo
    {
        // Marca (lila)
        public static readonly Color Acento = ColorTranslator.FromHtml("#C8A2C8");
        public static readonly Color Primario = ColorTranslator.FromHtml("#8A5A8A");        // botones y texto de acento (contraste AA con blanco)
        public static readonly Color PrimarioOscuro = ColorTranslator.FromHtml("#744A74");  // hover
        public static readonly Color PrimarioClaro = ColorTranslator.FromHtml("#F1E6F1");   // seleccion y hover suave
        public static readonly Color Foco = ColorTranslator.FromHtml("#B081B0");

        // Neutros
        public static readonly Color Fondo = ColorTranslator.FromHtml("#FAF7FA");
        public static readonly Color Borde = ColorTranslator.FromHtml("#EADFEA");
        public static readonly Color BordeCampo = ColorTranslator.FromHtml("#D9CFD9");
        public static readonly Color CampoDeshabilitado = ColorTranslator.FromHtml("#F6F1F6");
        public static readonly Color Texto = ColorTranslator.FromHtml("#3B3340");
        public static readonly Color Gris = ColorTranslator.FromHtml("#857B8A");

        // Pasteles de la referencia
        public static readonly Color ExitoPastel = ColorTranslator.FromHtml("#9DB89A");
        public static readonly Color ExitoBoton = ColorTranslator.FromHtml("#628A5F");
        public static readonly Color ExitoOscuro = ColorTranslator.FromHtml("#4F7A4C");
        public static readonly Color ExitoClaro = ColorTranslator.FromHtml("#E9F2E8");
        public static readonly Color Error = ColorTranslator.FromHtml("#E8A598");
        public static readonly Color ErrorBoton = ColorTranslator.FromHtml("#BF5B4E");
        public static readonly Color ErrorBorde = ColorTranslator.FromHtml("#D9766A");
        public static readonly Color ErrorTexto = ColorTranslator.FromHtml("#A4443A");
        public static readonly Color ErrorClaro = ColorTranslator.FromHtml("#FBE9E6");
        public static readonly Color AvisoPastel = ColorTranslator.FromHtml("#EBDCC3");
        public static readonly Color AvisoTexto = ColorTranslator.FromHtml("#85682F");
        public static readonly Color AvisoClaro = ColorTranslator.FromHtml("#F8F1E4");
        public static readonly Color InfoPastel = ColorTranslator.FromHtml("#7BA3C7");
        public static readonly Color InfoTexto = ColorTranslator.FromHtml("#3F6B91");
        public static readonly Color InfoClaro = ColorTranslator.FromHtml("#E7F0F7");

        public static readonly Font Normal = new Font("Segoe UI", 10F);
        public static readonly Font Pequena = new Font("Segoe UI", 9F);
        public static readonly Font Etiqueta = new Font("Segoe UI Semibold", 9.5F);
        public static readonly Font Subtitulo = new Font("Segoe UI Semibold", 12F);
        public static readonly Font Titulo = new Font("Segoe UI Semilight", 20F);
        public static readonly Font Cifra = new Font("Segoe UI Semibold", 17F);
        public static readonly Font Boton = new Font("Segoe UI Semibold", 10F);

        public const DataGridViewContentAlignment Izquierda = DataGridViewContentAlignment.MiddleLeft;
        public const DataGridViewContentAlignment Derecha = DataGridViewContentAlignment.MiddleRight;
        public const DataGridViewContentAlignment Centro = DataGridViewContentAlignment.MiddleCenter;

        // Rectangulo con esquinas redondeadas
        public static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // Aplica fondo, fuente y color base a un formulario
        public static void Formulario(Form f)
        {
            f.AutoScaleMode = AutoScaleMode.None;
            f.BackColor = Fondo;
            f.Font = Normal;
            f.ForeColor = Texto;
            f.StartPosition = FormStartPosition.CenterScreen;
        }

        // Ventana hija del MDI: centrada dentro del contenedor
        public static void Hijo(Form f)
        {
            Formulario(f);
            f.StartPosition = FormStartPosition.CenterParent;
        }

        // Encabezado de pantalla: icono en recuadro lila, titulo ligero y subtitulo gris
        public static Panel Encabezado(string icono, string titulo, string subtitulo)
        {
            Panel p = new Panel { Height = 92, BackColor = Fondo, Dock = DockStyle.Fill };
            p.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath ruta = Redondeado(new RectangleF(32, 22, 48, 48), 14))
                using (SolidBrush b = new SolidBrush(PrimarioClaro))
                    g.FillPath(b, ruta);
                Iconos.Dibujar(g, icono, 22, Primario, new RectangleF(32, 22, 48, 48));
                TextRenderer.DrawText(g, titulo, Titulo, new Point(94, 14), Texto, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, subtitulo, Pequena, new Point(96, 58), Gris, TextFormatFlags.NoPadding);
            };
            return p;
        }

        // Tabla de solo lectura con aspecto suave: encabezado claro, filas altas, seleccion lila y estado vacio
        public static DataGridView Tabla(string textoVacio = "No hay registros para mostrar.")
        {
            DataGridView g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, AllowUserToResizeColumns = false, MultiSelect = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                GridColor = Color.FromArgb(242, 236, 242), EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Tag = textoVacio
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Gris;
            g.ColumnHeadersDefaultCellStyle.Font = Etiqueta;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Gris;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.SelectionBackColor = PrimarioClaro;
            g.DefaultCellStyle.SelectionForeColor = Texto;
            g.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            g.RowTemplate.Height = 40;

            // Doble bufer (la propiedad es protegida)
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(g, true, null);

            // Resalta la fila bajo el mouse
            int hover = -1;
            Color colorHover = Color.FromArgb(251, 247, 251);
            Action<int, Color> pintar = (fila, color) =>
            {
                if (fila >= 0 && fila < g.Rows.Count) g.Rows[fila].DefaultCellStyle.BackColor = color;
            };
            g.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex == hover) return;
                pintar(hover, Color.Empty);
                hover = e.RowIndex;
                pintar(hover, colorHover);
            };
            g.MouseLeave += (s, e) => { pintar(hover, Color.Empty); hover = -1; };
            g.DataSourceChanged += (s, e) => hover = -1;

            // Mensaje cuando no hay filas
            g.Paint += (s, e) =>
            {
                if (g.Rows.Count > 0) return;
                Rectangle area = new Rectangle(16, g.ColumnHeadersHeight, Math.Max(10, g.Width - 32), Math.Max(10, g.Height - g.ColumnHeadersHeight));
                TextRenderer.DrawText(e.Graphics, g.Tag as string ?? "", Normal, area, Gris,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            };
            return g;
        }

        // Titulo, peso y formato de una columna (se ignora si no existe)
        public static void Columna(DataGridView g, string nombre, string titulo, float peso, string formato,
            DataGridViewContentAlignment alineacion, int anchoMinimo = 72)
        {
            if (!g.Columns.Contains(nombre)) return;
            DataGridViewColumn c = g.Columns[nombre];
            c.HeaderText = titulo;
            c.FillWeight = peso;
            c.MinimumWidth = anchoMinimo;
            c.DefaultCellStyle.Alignment = alineacion;
            c.HeaderCell.Style.Alignment = alineacion;
            if (formato != null) c.DefaultCellStyle.Format = formato;
        }

        // ---------- Mensajes ----------
        public static void Exito(IWin32Window dueno, string texto, string titulo)
        {
            Notificacion.Mostrar(dueno, TipoAviso.Exito, titulo, texto);
        }

        public static void Info(IWin32Window dueno, string texto, string titulo)
        {
            Notificacion.Mostrar(dueno, TipoAviso.Info, titulo, texto);
        }

        public static void Aviso(IWin32Window dueno, string texto, string titulo)
        {
            Notificacion.Mostrar(dueno, TipoAviso.Aviso, titulo, texto, 5000);
        }

        public static bool Confirmar(IWin32Window dueno, string texto, string titulo, string si = "Sí, continuar", bool peligro = false)
        {
            return Dialogo.Confirmar(dueno, titulo, texto, si, "Cancelar", peligro);
        }

        // Errores de base de datos: conexion o mensaje del servidor
        public static void ErrorBD(IWin32Window dueno, Exception ex)
        {
            MySqlException my = ex as MySqlException;
            if (my != null && (my.Number == 0 || my.Number == 1042 || my.Number == 1045))
                Dialogo.Error(dueno, "Error de conexión",
                    "No se pudo conectar con la base de datos.\nRevisa tu conexión a internet y los datos del archivo .env.");
            else
                Dialogo.Error(dueno, "Error de base de datos", "Ocurrió un error con la base de datos:\n" + ex.Message);
        }
    }
}
