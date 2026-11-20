using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Proyecto_facturas.Controles;

namespace Proyecto_facturas.Utilidades
{
    internal enum Filtro { Digitos, Decimal, Telefono }

    // Valida los campos de una pantalla: cada campo se revisa al salir de el y todos juntos al guardar.
    // El error aparece en linea, debajo del campo, y se quita solo cuando el valor queda correcto.
    // Las reglas concretas viven en la clase Reglas; ninguna pantalla define reglas propias.
    internal class Validador
    {
        private class Regla
        {
            public CampoBase Campo;
            public Func<string> Evaluar;
        }

        private readonly List<Regla> reglas = new List<Regla>();

        // Si es false, los campos solo se validan al pedirlo (por ejemplo, al pulsar un boton)
        public bool AlSalir { get; set; } = true;

        // Campo de texto con una regla
        public Validador Agregar(CampoTexto campo, Func<string, string> regla)
        {
            return Agregar((CampoBase)campo, () => regla(campo.Text));
        }

        // Lista desplegable obligatoria
        public Validador Agregar(CampoLista campo, string nombre)
        {
            return Agregar((CampoBase)campo, () => campo.SelectedIndex < 0 ? "Selecciona " + nombre + "." : null);
        }

        // Rango de fechas: "desde" no puede ser mayor que "hasta" ni haber mas de 5 anos de diferencia
        public Validador AgregarRango(CampoFecha desde, CampoFecha hasta)
        {
            Agregar((CampoBase)hasta, () =>
            {
                if (desde.Value.Date > hasta.Value.Date) return "No puede ser menor a 'Desde'.";
                if ((hasta.Value.Date - desde.Value.Date).TotalDays > 366 * 5) return "Rango máximo: 5 años.";
                return null;
            });
            desde.ValorCambiado += (s, e) => Validar(hasta);
            return this;
        }

        // Regla libre sobre cualquier campo (devuelve el mensaje de error o null)
        public Validador Agregar(CampoBase campo, Func<string> regla)
        {
            reglas.Add(new Regla { Campo = campo, Evaluar = regla });
            campo.Interior.Validating += (s, e) => { if (AlSalir) Validar(campo); };
            // Si el campo ya muestra un error, se revalida mientras el usuario lo corrige
            campo.ValorCambiado += (s, e) => { if (AlSalir && campo.Error.Length > 0) Validar(campo); };
            return this;
        }

        // Valida un solo campo y muestra u oculta su mensaje de error
        public bool Validar(CampoBase campo)
        {
            string mensaje = null;
            foreach (Regla r in reglas)
                if (r.Campo == campo && (mensaje = r.Evaluar()) != null) break;

            campo.MostrarError(mensaje);
            return mensaje == null;
        }

        // Valida todos los campos sin detenerse en el primero que falle y enfoca el primero con error
        public bool ValidarTodo()
        {
            bool ok = true;
            CampoBase primero = null;
            foreach (CampoBase c in reglas.Select(r => r.Campo).Distinct().ToList())
            {
                if (!c.Enabled || !c.Visible) continue;
                if (Validar(c)) continue;

                ok = false;
                if (primero == null || Posicion(c) < Posicion(primero)) primero = c;
            }
            if (primero != null) primero.Interior.Focus();
            return ok;
        }

        private static long Posicion(Control c)
        {
            Point p = c.PointToScreen(Point.Empty);
            return (long)p.Y * 10000 + p.X;
        }

        // Marca un error que viene de otra validacion (por ejemplo, un duplicado detectado por la base)
        public void Marcar(CampoBase campo, string mensaje)
        {
            campo.MostrarError(mensaje);
        }

        public void Limpiar()
        {
            foreach (CampoBase c in reglas.Select(r => r.Campo).Distinct())
                c.MostrarError("");
        }

        // Restringe lo que se puede escribir en un campo de texto
        public static void Filtrar(CampoTexto campo, Filtro filtro)
        {
            TextBox caja = campo.Caja;
            caja.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                bool permitido;
                switch (filtro)
                {
                    case Filtro.Digitos: permitido = char.IsDigit(e.KeyChar); break;
                    case Filtro.Decimal: permitido = char.IsDigit(e.KeyChar) || (e.KeyChar == '.' && !caja.Text.Contains(".")); break;
                    default: permitido = char.IsDigit(e.KeyChar) || " -+()".IndexOf(e.KeyChar) >= 0; break;
                }
                e.Handled = !permitido;
            };
        }
    }
}
