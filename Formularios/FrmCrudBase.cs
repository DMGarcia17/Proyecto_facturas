using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas.Formularios
{
    internal enum Modo { Nuevo, Consultar, Editar, Eliminar }

    // Formulario base del CRUD: un solo diseno para Nuevo, Consultar, Editar y Eliminar.
    // Cada entidad (productos, clientes, usuarios) solo define sus campos, su consulta y su guardado.
    internal abstract class FrmCrudBase : Form
    {
        protected readonly Modo modo;
        protected readonly Validador validador = new Validador();
        protected DataGridView dgv;
        protected int idActual;

        private readonly string singular, plural;
        private TextBox txtBuscar;
        private Button btnGuardar, btnEliminar;
        private readonly Timer temporizador = new Timer { Interval = 350 };
        private bool cargando;

        protected FrmCrudBase(Modo modo, string singular, string plural)
        {
            this.modo = modo;
            this.singular = singular;
            this.plural = plural;

            Estilo.Hijo(this);
            ClientSize = new Size(940, 640);
            MinimumSize = new Size(940, 640);

            string titulo, subtitulo;
            switch (modo)
            {
                case Modo.Nuevo:
                    titulo = "Nuevo " + singular; subtitulo = "Completa los datos y guarda el registro"; break;
                case Modo.Editar:
                    titulo = "Editar " + singular; subtitulo = "Selecciona un registro de la lista, modifica sus datos y guarda"; break;
                case Modo.Eliminar:
                    titulo = "Eliminar " + singular; subtitulo = "Selecciona un registro de la lista y confírmalo para eliminarlo"; break;
                default:
                    titulo = "Consultar " + plural; subtitulo = "Busca y revisa los registros existentes"; break;
            }
            Text = titulo;
            Estilo.Encabezado(this, titulo, subtitulo);

            temporizador.Tick += (s, e) => { temporizador.Stop(); Cargar(); };
            FormClosed += (s, e) => temporizador.Dispose();
        }

        // ---- Lo que define cada entidad ----
        protected abstract string ColumnaId { get; }
        protected abstract int CrearCampos(int y);                 // dibuja los campos y devuelve el alto usado
        protected abstract DataTable Listar(string filtro);
        protected abstract void FormatoGrilla();
        protected abstract void CargarCampos(DataGridViewRow fila);
        protected abstract void LimpiarCampos();
        protected abstract void HabilitarCampos(bool habilitado);
        protected abstract string Guardar(bool nuevo);            // null si salio bien, o el mensaje de error
        protected abstract string EliminarRegistro(int id);
        protected abstract string Resumen(DataGridViewRow fila);  // texto para confirmar la eliminacion

        // Etiqueta con su caja de texto (fila de campos en la posicion y)
        protected TextBox Campo(string etiqueta, int x, int y, int ancho, int maxLength)
        {
            Estilo.NuevaEtiqueta(this, etiqueta, x, y);
            return Estilo.NuevaCaja(this, x, y + 24, ancho, maxLength);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            int y = 106;
            if (modo != Modo.Consultar)
            {
                y += CrearCampos(y);
                CrearBotones(y);
                y += 56;
            }

            Label lista = Estilo.NuevaEtiqueta(this, "Registros", 24, y + 6);
            lista.Font = Estilo.Subtitulo;

            Label buscar = Estilo.NuevaEtiqueta(this, "Buscar:", ClientSize.Width - 330, y + 8);
            buscar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtBuscar = Estilo.NuevaCaja(this, ClientSize.Width - 270, y + 4, 246, 100);
            txtBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtBuscar.TextChanged += (s, ev) => { temporizador.Stop(); temporizador.Start(); };

            y += 38;
            dgv = Estilo.NuevaTabla(this, 24, y, ClientSize.Width - 48, ClientSize.Height - y - 24);
            dgv.SelectionChanged += dgv_SelectionChanged;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Cargar();
            if (modo == Modo.Nuevo) HabilitarCampos(true);
        }

        private void CrearBotones(int y)
        {
            switch (modo)
            {
                case Modo.Nuevo:
                    btnGuardar = Estilo.NuevoBoton(this, "Guardar", 24, y, 130, true);
                    Button limpiar = Estilo.NuevoBoton(this, "Limpiar", 164, y, 110, false);
                    limpiar.CausesValidation = false;
                    limpiar.Click += (s, e) => { LimpiarCampos(); validador.Limpiar(); };
                    AcceptButton = btnGuardar;
                    break;
                case Modo.Editar:
                    btnGuardar = Estilo.NuevoBoton(this, "Guardar cambios", 24, y, 160, true);
                    btnGuardar.Enabled = false;
                    Button cancelar = Estilo.NuevoBoton(this, "Cancelar", 194, y, 110, false);
                    cancelar.CausesValidation = false;
                    cancelar.Click += (s, e) => Deseleccionar();
                    AcceptButton = btnGuardar;
                    break;
                case Modo.Eliminar:
                    btnEliminar = Estilo.NuevoBoton(this, "Eliminar", 24, y, 130, false);
                    Estilo.BotonPeligro(btnEliminar);
                    btnEliminar.Enabled = false;
                    btnEliminar.Click += btnEliminar_Click;
                    break;
            }
            if (btnGuardar != null) btnGuardar.Click += btnGuardar_Click;
        }

        // Recarga la lista segun el texto de busqueda
        protected void Cargar()
        {
            try
            {
                cargando = true;
                dgv.DataSource = Listar(txtBuscar.Text);
                FormatoGrilla();
                dgv.ClearSelection();
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
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (modo == Modo.Editar && idActual == 0) return;

            if (!validador.ValidarTodo())
            {
                Estilo.Aviso(this, "Corrige los campos marcados con el icono rojo.", "Datos incompletos");
                return;
            }

            try
            {
                string error = Guardar(modo == Modo.Nuevo);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo guardar");
                    return;
                }

                Estilo.Info(this, modo == Modo.Nuevo ? "El registro se guardó correctamente." : "Los cambios se guardaron correctamente.",
                    modo == Modo.Nuevo ? "Registro creado" : "Registro actualizado");
                LimpiarCampos();
                validador.Limpiar();
                Cargar();
                if (modo == Modo.Nuevo) HabilitarCampos(true);
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
            if (!Estilo.Confirmar(this, "¿Seguro que quieres eliminar " + singular + " \"" + resumen + "\"?\nEsta acción no se puede deshacer.",
                    "Confirmar eliminación"))
                return;

            try
            {
                string error = EliminarRegistro(idActual);
                if (error != null)
                {
                    Estilo.Aviso(this, error, "No se pudo eliminar");
                    return;
                }

                Estilo.Info(this, "El registro se eliminó correctamente.", "Registro eliminado");
                Cargar();
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Estilo.ErrorBD(this, ex);
            }
        }
    }
}
