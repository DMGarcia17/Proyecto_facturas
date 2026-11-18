using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Proyecto_facturas.Utilidades
{
    // Colores y estilos compartidos (los mismos de Proyecto_productos) para que todas las pantallas se vean iguales
    internal static class Estilo
    {
        public static readonly Color Primario = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimarioOscuro = Color.FromArgb(29, 78, 216);
        public static readonly Color PrimarioClaro = Color.FromArgb(219, 234, 254);
        public static readonly Color Fondo = Color.FromArgb(245, 247, 251);
        public static readonly Color Texto = Color.FromArgb(31, 41, 55);
        public static readonly Color Gris = Color.FromArgb(107, 114, 128);
        public static readonly Color Error = Color.FromArgb(220, 38, 38);
        public static readonly Color ErrorClaro = Color.FromArgb(254, 226, 226);
        public static readonly Color ErrorOscuro = Color.FromArgb(153, 27, 27);

        public static readonly Font Normal = new Font("Segoe UI", 10F);
        public static readonly Font Etiqueta = new Font("Segoe UI Semibold", 9.5F);
        public static readonly Font Subtitulo = new Font("Segoe UI Semibold", 11F);

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

        // Franja azul superior con titulo y subtitulo
        public static Panel Encabezado(Form f, string titulo, string subtitulo)
        {
            Panel p = new Panel { BackColor = Primario, Dock = DockStyle.Top, Height = 90 };
            p.Controls.Add(new Label
            {
                AutoSize = true, Text = titulo, ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 18F), Location = new Point(24, 12)
            });
            p.Controls.Add(new Label
            {
                AutoSize = true, Text = subtitulo, ForeColor = Color.FromArgb(219, 234, 254),
                Font = new Font("Segoe UI", 9.5F), Location = new Point(27, 54)
            });
            f.Controls.Add(p);
            return p;
        }

        public static Label NuevaEtiqueta(Control padre, string texto, int x, int y)
        {
            Label l = new Label { AutoSize = true, Text = texto, Font = Etiqueta, Location = new Point(x, y) };
            padre.Controls.Add(l);
            return l;
        }

        public static TextBox NuevaCaja(Control padre, int x, int y, int ancho, int maxLength)
        {
            TextBox t = new TextBox { Location = new Point(x, y), Width = ancho, MaxLength = maxLength };
            padre.Controls.Add(t);
            return t;
        }

        // Lista desplegable de solo seleccion
        public static ComboBox NuevaCombo(Control padre, int x, int y, int ancho)
        {
            ComboBox c = new ComboBox
            {
                Location = new Point(x, y), Width = ancho, DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat, BackColor = Color.White
            };
            padre.Controls.Add(c);
            return c;
        }

        public static DateTimePicker NuevaFecha(Control padre, int x, int y, int ancho)
        {
            DateTimePicker d = new DateTimePicker
            {
                Location = new Point(x, y), Width = ancho, Format = DateTimePickerFormat.Short
            };
            padre.Controls.Add(d);
            return d;
        }

        public static Button NuevoBoton(Control padre, string texto, int x, int y, int ancho, bool primario)
        {
            Button b = new Button { Text = texto, Location = new Point(x, y), Size = new Size(ancho, 40) };
            if (primario) BotonPrimario(b); else BotonSecundario(b);
            padre.Controls.Add(b);
            return b;
        }

        // Tabla de solo lectura con el estilo azul
        public static DataGridView NuevaTabla(Control padre, int x, int y, int ancho, int alto)
        {
            DataGridView g = new DataGridView
            {
                Location = new Point(x, y), Size = new Size(ancho, alto),
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, MultiSelect = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(229, 231, 235), EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Primario;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Etiqueta;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primario;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            g.DefaultCellStyle.SelectionBackColor = PrimarioClaro;
            g.DefaultCellStyle.SelectionForeColor = Texto;
            g.RowTemplate.Height = 28;
            padre.Controls.Add(g);
            return g;
        }

        // Titulo, peso y formato de una columna (se ignora si no existe)
        public static void Columna(DataGridView g, string nombre, string titulo, float peso, string formato,
            DataGridViewContentAlignment alineacion)
        {
            if (!g.Columns.Contains(nombre)) return;
            DataGridViewColumn c = g.Columns[nombre];
            c.HeaderText = titulo;
            c.FillWeight = peso;
            c.DefaultCellStyle.Alignment = alineacion;
            c.HeaderCell.Style.Alignment = alineacion;
            if (formato != null) c.DefaultCellStyle.Format = formato;
        }

        public const DataGridViewContentAlignment Izquierda = DataGridViewContentAlignment.MiddleLeft;
        public const DataGridViewContentAlignment Derecha = DataGridViewContentAlignment.MiddleRight;
        public const DataGridViewContentAlignment Centro = DataGridViewContentAlignment.MiddleCenter;

        // Tarjeta blanca con una cifra grande y su titulo; devuelve la etiqueta de la cifra
        public static Label TarjetaResumen(Control padre, string titulo, int x, int y, int ancho, Color acento)
        {
            Panel p = new Panel { Location = new Point(x, y), Size = new Size(ancho, 70), BackColor = Color.White };
            p.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = acento });
            Label cifra = new Label
            {
                AutoSize = false, Font = new Font("Segoe UI Semibold", 15F), ForeColor = Texto,
                Location = new Point(16, 6), Size = new Size(ancho - 22, 34), Text = "-"
            };
            Label caption = new Label
            {
                AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Gris, Location = new Point(17, 44), Text = titulo
            };
            p.Controls.Add(cifra);
            p.Controls.Add(caption);
            padre.Controls.Add(p);
            return cifra;
        }

        // Errores de base de datos: conexion o mensaje del servidor
        public static void ErrorBD(IWin32Window dueno, Exception ex)
        {
            MySqlException my = ex as MySqlException;
            if (my != null && (my.Number == 0 || my.Number == 1042 || my.Number == 1045))
                MessageBox.Show(dueno, "No se pudo conectar con la base de datos.\nRevisa tu conexión a internet y los datos del archivo .env.",
                    "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show(dueno, "Ocurrió un error con la base de datos:\n" + ex.Message,
                    "Error de base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Aviso(IWin32Window dueno, string texto, string titulo)
        {
            MessageBox.Show(dueno, texto, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Info(IWin32Window dueno, string texto, string titulo)
        {
            MessageBox.Show(dueno, texto, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static bool Confirmar(IWin32Window dueno, string texto, string titulo)
        {
            return MessageBox.Show(dueno, texto, titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        public static void BotonPrimario(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Primario;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI Semibold", 10.5F);
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = PrimarioOscuro;
            // Deshabilitado: azul claro para que se note que no se puede usar
            b.EnabledChanged += (s, e) => b.BackColor = b.Enabled ? Primario : Color.FromArgb(191, 219, 254);
        }

        public static void BotonSecundario(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            b.BackColor = Color.White;
            b.ForeColor = Texto;
            b.Font = new Font("Segoe UI Semibold", 10.5F);
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 244, 246);
        }

        public static void BotonPeligro(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Error;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI Semibold", 10.5F);
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            b.EnabledChanged += (s, e) => b.BackColor = b.Enabled ? Error : Color.FromArgb(252, 205, 205);
        }
    }
}
