using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Proyecto_facturas.Configuracion;
using Proyecto_facturas.Datos;
using Proyecto_facturas.Formularios;
using Proyecto_facturas.Utilidades;

namespace Proyecto_facturas
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            FijarCulturaUsd();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1) Lee el .env y prepara la base de datos (en una base limpia crea tablas y datos iniciales)
            Exception error;
            try
            {
                EnvLoader.Cargar();
                using (FrmCarga carga = new FrmCarga(InicializadorBD.Ejecutar))
                {
                    carga.ShowDialog();
                    error = carga.Error;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                MostrarErrorInicio(error);
                return;
            }

            // 2) Login -> ventana principal. "Cerrar sesion" vuelve al login; cualquier otra salida termina.
            while (true)
            {
                using (FrmLogin login = new FrmLogin())
                    if (login.ShowDialog() != DialogResult.OK) return;

                using (FrmPrincipal principal = new FrmPrincipal())
                {
                    Application.Run(principal);
                    if (!principal.CerrarSesionSolicitada) return;
                }
            }
        }

        // Los importes siempre se muestran en dolares ($1,234.56), sin depender de la region de Windows
        // (que podria mostrar otro simbolo de moneda). Aplica a tablas, tarjetas, combos y reportes.
        private static void FijarCulturaUsd()
        {
            CultureInfo usd = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
            usd.NumberFormat.CurrencySymbol = "$";
            usd.NumberFormat.CurrencyDecimalDigits = 2;
            usd.NumberFormat.CurrencyPositivePattern = 0;  // $n
            usd.NumberFormat.CurrencyNegativePattern = 1;  // -$n

            CultureInfo.DefaultThreadCurrentCulture = usd;
            CultureInfo.DefaultThreadCurrentUICulture = usd;
            Thread.CurrentThread.CurrentCulture = usd;
            Thread.CurrentThread.CurrentUICulture = usd;
        }

        private static void MostrarErrorInicio(Exception error)
        {
            string mensaje = error is MySqlException
                ? "No se pudo conectar o preparar la base de datos.\nRevisa tu conexión y los datos del archivo .env.\n\nDetalle: " + error.Message
                : error.Message;
            MessageBox.Show(mensaje, "No se pudo iniciar la aplicación", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
