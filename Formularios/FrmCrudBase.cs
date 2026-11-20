using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Proyecto_facturas.Controles;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    internal enum Modo { Nuevo, Consultar, Editar, Eliminar }

    // Formulario base del CRUD. Un solo diseno para Nuevo, Consultar, Editar y Eliminar:
    //  - Nuevo:     tarjeta de formulario por secciones + tarjeta con los registros existentes
    //  - Editar:    lista a la izquierda y formulario de detalle a la derecha (maestro-detalle)
    //  - Eliminar:  igual que Editar, con los datos en solo lectura y un aviso antes de borrar
    //  - Consultar: lista a ancho completo con buscador
    // Cada entidad solo define sus campos (CrearFormulario), su consulta y su guardado.
    internal abstract class FrmCrudBase : Form
    {
        protected readonly Modo modo;
        protected readonly Validador validador = new Validador();
        protected DataGridView dgv;
        protected int idActual;

        private readonly string singular, plural;
        private CampoTexto txtBuscar;
        private Label lblContador, lblAviso;
        private Panel avisoSeleccion;
        private BotonPildora btnGuardar, btnEliminar;
        private readonly Timer temporizador = new Timer { Interval = 350 };
        private bool cargando;

        protected FrmCrudBase(Modo modo, string singular, string plural)
        {
            this.modo = modo;
            this.singular = singular;
            this.plural = plural;

            Estilo.Hijo(this);
            ClientSize = new Size(1040, 700);
            MinimumSize = new Size(900, 600);

            temporizador.Tick += (s, e) => { temporizador.Stop(); Cargar(); };
            FormClosed += (s, e) => temporizador.Dispose();
        }

        // ---- Lo que define cada entidad ----
        protected abstract string ColumnaId { get; }
        protected abstract string IconoEntidad { get; }
        protected abstract Control CrearFormulario();               // campos organizados en secciones (usar Apilar)
        protected abstract DataTable Listar(string filtro);
        protected abstract void FormatoGrilla();
        protected abstract void CargarCampos(DataGridViewRow fila);
        protected abstract void LimpiarCampos();
        protected abstract void HabilitarCampos(bool habilitado);
        protected abstract void EnfocarPrimero();
        protected abstract string Guardar(bool nuevo);              // null si salio bien, o el mensaje de error
        protected abstract string EliminarRegistro(int id);
        protected abstract string Resumen(DataGridViewRow fila);    // texto para confirmar la eliminacion

        // Permite marcar el campo concreto cuando la base devuelve un duplicado. True si lo manejo.
        protected virtual bool MarcarErrorServidor(string error) { return false; }

        // Apila las secciones del formulario en un panel con desplazamiento
        protected static Control Apilar(params SeccionForm[] secciones)
        {
            Panel contenedor = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            TableLayoutPanel pila = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Color.White, Padding = new Padding(0, 0, 12, 0)
            };
            pila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (SeccionForm s in secciones)
            {
                pila.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                s.Dock = DockStyle.Fill;
                pila.Controls.Add(s, 0, pila.RowCount++);
            }
            contenedor.Controls.Add(pila);
            return contenedor;
        }

        private string TituloPantalla
        {
            get
            {
                switch (modo)
                {
                    case Modo.Nuevo: return "Nuevo " + singular;
                    case Modo.Editar: return "Editar " + singular;
                    case Modo.Eliminar: return "Eliminar " + singular;
                    default: return "Consultar " + plural;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Text = TituloPantalla;
            string subtitulo;
            switch (modo)
            {
                case Modo.Nuevo: subtitulo = "Completa los datos y guarda el registro. Los campos con * son obligatorios."; break;
                case Modo.Editar: subtitulo = "Elige un registro de la lista, modifica sus datos y guarda los cambios."; break;
                case Modo.Eliminar: subtitulo = "Elige un registro de la lista y confírmalo para eliminarlo."; break;
                default: subtitulo = "Busca y revisa los registros existentes."; break;
            }

            TableLayoutPanel cuerpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12), ColumnCount = modo == Modo.Consultar ? 1 : 2, RowCount = 1
            };
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Tarjeta lista = CrearTarjetaLista();
            if (modo == Modo.Consultar)
            {
                cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                cuerpo.Controls.Add(lista, 0, 0);
            }
            else
            {
                Tarjeta formulario = CrearTarjetaFormulario();
                bool listaIzquierda = modo != Modo.Nuevo;
                cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, listaIzquierda ? 42 : 58));
                cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, listaIzquierda ? 58 : 42));
                cuerpo.Controls.Add(listaIzquierda ? lista : formulario, 0, 0);
                cuerpo.Controls.Add(listaIzquierda ? formulario : lista, 1, 0);
            }

            Controls.Add(Disposicion.Pila(1, Estilo.Encabezado(IconoEntidad, TituloPantalla, subtitulo), cuerpo));
        }

        // Tarjeta con buscador, contador de registros y la tabla
        private Tarjeta CrearTarjetaLista()
        {
            Tarjeta t = new Tarjeta { Dock = DockStyle.Fill, Titulo = modo == Modo.Nuevo ? "Registros existentes" : "Registros", Icono = Iconos.Etiqueta };

            txtBuscar = new CampoTexto { Compacto = true, Icono = Iconos.Buscar, Placeholder = "Buscar...", Dock = DockStyle.Fill };
            txtBuscar.Caja.TextChanged += (s, e) => { temporizador.Stop(); temporizador.Start(); };
            lblContador = new Label
            {
                AutoSize = false, Width = 110, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Estilo.Gris, Font = Estilo.Pequena
            };
            Panel busqueda = new Panel { Height = 52, Padding = new Padding(0, 4, 0, 6) };
            busqueda.Controls.Add(txtBuscar);
            busqueda.Controls.Add(lblContador);

            dgv = Estilo.Tabla("No se encontraron registros.");
            dgv.SelectionChanged += dgv_SelectionChanged;
            t.Controls.Add(Disposicion.Pila(1, busqueda, dgv));
            return t;
        }

        // Tarjeta con el formulario por secciones y la barra de acciones
        private Tarjeta CrearTarjetaFormulario()
        {
            string titulo = modo == Modo.Nuevo ? "Datos del " + singular : modo == Modo.Editar ? "Editar datos" : "Registro seleccionado";
            Tarjeta t = new Tarjeta { Dock = DockStyle.Fill, Titulo = titulo, Icono = modo == Modo.Eliminar ? Iconos.Eliminar : modo == Modo.Editar ? Iconos.Editar : Iconos.Agregar };

            Control formulario = CrearFormulario();
            formulario.Dock = DockStyle.Fill;

            // Aviso superior: indica que hay que elegir un registro (editar/eliminar)
            Control aviso = null;
            if (modo != Modo.Nuevo)
            {
                bool eliminar = modo == Modo.Eliminar;
                // Recuadro redondeado azul grisaceo (se pinta a mano para tener esquinas suaves)
                avisoSeleccion = new Panel { Height = 48, BackColor = Color.White, Margin = new Padding(0, 0, 0, 12) };
                avisoSeleccion.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (System.Drawing.Drawing2D.GraphicsPath p = Estilo.Redondeado(new RectangleF(0, 0, avisoSeleccion.Width - 1, avisoSeleccion.Height - 1), 12))
                    using (SolidBrush b = new SolidBrush(Estilo.InfoClaro))
                        e.Graphics.FillPath(b, p);
                };
                lblAviso = new Label
                {
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Estilo.InfoTexto, Font = Estilo.Pequena,
                    Padding = new Padding(16, 0, 8, 0), BackColor = Color.Transparent,
                    Text = "Selecciona " + (singular == "factura" ? "una " : "un ") + singular + " de la lista para " + (eliminar ? "eliminarlo." : "editarlo.")
                };
                avisoSeleccion.Controls.Add(lblAviso);
                aviso = avisoSeleccion;
            }

            // Barra de acciones
            FlowLayoutPanel acciones = new FlowLayoutPanel { Height = 60, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 12, 0, 0) };
            switch (modo)
            {
                case Modo.Nuevo:
                    btnGuardar = new BotonPildora { Text = "Guardar", Icono = Iconos.Guardar, Variante = Variante.Exito, Width = 150 };
                    BotonPildora limpiar = new BotonPildora { Text = "Limpiar", Icono = Iconos.Limpiar, Variante = Variante.Secundario, Width = 130, CausesValidation = false };
                    limpiar.Click += (s, e) => { LimpiarCampos(); validador.Limpiar(); EnfocarPrimero(); };
                    acciones.Controls.AddRange(new Control[] { btnGuardar, limpiar });
                    AcceptButton = btnGuardar;
                    break;
                case Modo.Editar:
                    btnGuardar = new BotonPildora { Text = "Guardar cambios", Icono = Iconos.Guardar, Variante = Variante.Exito, Width = 190, Enabled = false };
                    BotonPildora cancelar = new BotonPildora { Text = "Cancelar", Variante = Variante.Secundario, Width = 130, CausesValidation = false };
                    cancelar.Click += (s, e) => Deseleccionar();
                    acciones.Controls.AddRange(new Control[] { btnGuardar, cancelar });
                    AcceptButton = btnGuardar;
                    break;
                case Modo.Eliminar:
                    btnEliminar = new BotonPildora { Text = "Eliminar " + singular, Icono = Iconos.Eliminar, Variante = Variante.Peligro, Width = 200, Enabled = false };
                    btnEliminar.Click += btnEliminar_Click;
                    acciones.Controls.Add(btnEliminar);
                    break;
            }
            if (btnGuardar != null) btnGuardar.Click += btnGuardar_Click;

            if (aviso != null) t.Controls.Add(Disposicion.Pila(1, aviso, formulario, acciones));
            else t.Controls.Add(Disposicion.Pila(0, formulario, acciones));
            return t;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Cargar();
            if (modo == Modo.Nuevo) { HabilitarCampos(true); EnfocarPrimero(); }
            else if (modo != Modo.Consultar) HabilitarCampos(false);
        }

        // Recarga la lista segun el texto de busqueda
        protected void Cargar()
        {
            try
            {
                cargando = true;
                dgv.DataSource = Listar(txtBuscar.Text);
                FormatoGrilla();
                int n = dgv.Rows.Count;
                lblContador.Text = n + (n == 1 ? " registro" : " registros");
                cargando = false;
                Deseleccionar();
            }
            catch (Exception ex) when (ex is MySql.Data.MySqlClient.MySqlException || ex is InvalidOperationException)
            {
                cargando = false;
                Estilo.ErrorBD(this, ex);
            }
        }

        // Quita la seleccion y deja los campos listos segun el modo
        private void Deseleccionar()
        {
            cargando = true;
            dgv.ClearSelection();
            cargando = false;
            idActual = 0;

            if (modo == Modo.Editar || modo == Modo.Eliminar)
            {
                LimpiarCampos();
                HabilitarCampos(false);
                if (btnGuardar != null) btnGuardar.Enabled = false;
                if (btnEliminar != null) btnEliminar.Enabled = false;
                if (avisoSeleccion != null) avisoSeleccion.Visible = true;
            }
            validador.Limpiar();
        }

        private void dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (cargando || modo == Modo.Nuevo || modo == Modo.Consultar) return;
            if (dgv.SelectedRows.Count == 0) return;

            DataGridViewRow fila = dgv.SelectedRows[0];
            idActual = Convert.ToInt32(fila.Cells[ColumnaId].Value);
            CargarCampos(fila);
            validador.Limpiar();
            HabilitarCampos(modo == Modo.Editar);
            if (btnGuardar != null) btnGuardar.Enabled = true;
            if (btnEliminar != null) btnEliminar.Enabled = true;
            if (avisoSeleccion != null) avisoSeleccion.Visible = false;
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (modo == Modo.Editar && idActual == 0) return;

            if (!validador.ValidarTodo())
            {
                Estilo.Aviso(this, "Revisa los campos marcados en rojo antes de continuar.", "Faltan datos");
                return;
            }

            try
            {
                string error = Guardar(modo == Modo.Nuevo);
                if (error != null)
                {
                    MarcarErrorServidor(error);
                    Estilo.Aviso(this, error, "No se pudo guardar");
                    return;
                }

                Estilo.Exito(this, modo == Modo.Nuevo ? "El registro se guardó correctamente." : "Los cambios se guardaron correctamente.",
                    modo == Modo.Nuevo ? "Registro creado" : "Registro actualizado");
                LimpiarCampos();
                validador.Limpiar();
                Cargar();
                if (modo == Modo.Nuevo) { HabilitarCampos(true); EnfocarPrimero(); }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (idActual == 0 || dgv.SelectedRows.Count == 0) return;

            string resumen = Resumen(dgv.SelectedRows[0]);
            if (!Estilo.Confirmar(this, "Vas a eliminar " + singular + " \"" + resumen + "\".\nEsta acción no se puede deshacer.",
                    "¿Eliminar este registro?", "Sí, eliminar", true))
                return;

            try
            {
                string error = EliminarRegistro(idActual);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo eliminar");
                    return;
                }

                Estilo.Exito(this, "El registro se eliminó correctamente.", "Registro eliminado");
                Cargar();
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }
    }
}
