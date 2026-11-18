using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Proyecto_facturas.Utilidades
{
    // Reglas de validacion reutilizables. Cada regla recibe el texto y devuelve el mensaje de error
    // o null si es valida. Ninguna pantalla define reglas propias: todas usan estas.
    internal static class Reglas
    {
        private static readonly Regex PatronEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex PatronUsuario = new Regex(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex PatronDecimal = new Regex(@"^\d{1,9}(\.\d{1,2})?$", RegexOptions.Compiled);

        public static Func<string, string> Requerido(string nombre)
        {
            return t => string.IsNullOrWhiteSpace(t) ? "Escribe " + nombre + "." : null;
        }

        // Texto de min a max caracteres (sin contar espacios de los extremos)
        public static Func<string, string> Texto(string nombre, int min, int max, bool exigirLetra = true)
        {
            return t =>
            {
                string v = (t ?? "").Trim();
                if (v.Length == 0) return "Escribe " + nombre + ".";
                if (v.Length < min || v.Length > max)
                    return nombre.Substring(0, 1).ToUpper() + nombre.Substring(1) + " debe tener entre " + min + " y " + max + " caracteres.";
                if (exigirLetra && !v.Any(char.IsLetter)) return nombre.Substring(0, 1).ToUpper() + nombre.Substring(1) + " debe incluir letras.";
                return null;
            };
        }

        // Hace opcional otra regla: si el campo esta vacio es valido
        public static Func<string, string> Opcional(Func<string, string> regla)
        {
            return t => string.IsNullOrWhiteSpace(t) ? null : regla(t);
        }

        public static Func<string, string> Email()
        {
            return t =>
            {
                string v = (t ?? "").Trim();
                if (v.Length > 150 || !PatronEmail.IsMatch(v)) return "Escribe un correo válido (ejemplo: nombre@dominio.com).";
                return null;
            };
        }

        // De 8 a 15 digitos; se aceptan espacios, guiones, parentesis y + como separadores
        public static Func<string, string> Telefono()
        {
            return t =>
            {
                string v = (t ?? "").Trim();
                int digitos = v.Count(char.IsDigit);
                bool caracteres = v.All(c => char.IsDigit(c) || " -+()".IndexOf(c) >= 0);
                if (v.Length == 0) return "Escribe el teléfono.";
                if (!caracteres || digitos < 8 || digitos > 15) return "Escribe un teléfono válido (de 8 a 15 dígitos).";
                return null;
            };
        }

        public static Func<string, string> Usuario()
        {
            return t => PatronUsuario.IsMatch((t ?? "").Trim())
                ? null
                : "El usuario debe tener de 3 a 50 caracteres (letras, números, punto, guion o guion bajo).";
        }

        // De 8 a 64 caracteres, con al menos una letra y un numero
        public static Func<string, string> Contrasena()
        {
            return t =>
            {
                string v = t ?? "";
                if (v.Length < 8 || v.Length > 64 || !v.Any(char.IsLetter) || !v.Any(char.IsDigit))
                    return "La contraseña debe tener de 8 a 64 caracteres, con letras y números.";
                return null;
            };
        }

        // Debe ser igual al valor que devuelve "otro" (por ejemplo, confirmar contrasena)
        public static Func<string, string> Coincide(Func<string> otro, string mensaje)
        {
            return t => string.Equals(t, otro(), StringComparison.Ordinal) ? null : mensaje;
        }

        // Entero entre min y max (el maximo se evalua al validar, por ejemplo el stock disponible)
        public static Func<string, string> Entero(string nombre, int min, Func<int> max)
        {
            return t =>
            {
                if (!EsEntero(t, out int v)) return "Escribe " + nombre + " como número entero.";
                int tope = max();
                if (v < min) return Capitalizar(nombre) + " debe ser " + min + " o más.";
                if (v > tope) return Capitalizar(nombre) + " no puede ser mayor a " + tope.ToString("N0") + ".";
                return null;
            };
        }

        public static Func<string, string> Entero(string nombre, int min, int max)
        {
            return Entero(nombre, min, () => max);
        }

        // Numero mayor a cero con punto decimal y hasta 2 decimales
        public static Func<string, string> Dinero(string nombre)
        {
            return t =>
            {
                if (!PatronDecimal.IsMatch((t ?? "").Trim()) || ParseDecimal(t) <= 0)
                    return "Escribe " + nombre + " mayor a cero (hasta 2 decimales, con punto).";
                return null;
            };
        }

        private static string Capitalizar(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        public static bool EsEntero(string texto, out int valor)
        {
            valor = 0;
            return Regex.IsMatch((texto ?? "").Trim(), @"^\d{1,9}$")
                && int.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out valor);
        }

        public static double ParseDecimal(string texto)
        {
            return double.TryParse((texto ?? "").Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }
    }

    internal enum Filtro { Digitos, Decimal, Telefono }

    // Valida los controles de una pantalla: cada campo se revisa al salir de el y todos juntos al guardar.
    // Muestra el icono de error junto al campo.
    internal class Validador
    {
        private readonly ErrorProvider errores = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private readonly List<KeyValuePair<Control, Func<string>>> reglas = new List<KeyValuePair<Control, Func<string>>>();

        // Si es false, los campos solo se validan al pedirlo (por ejemplo, al pulsar un boton)
        public bool AlSalir { get; set; } = true;

        // Campo de texto con una regla
        public Validador Agregar(TextBox caja, Func<string, string> regla)
        {
            return Agregar(caja, () => regla(caja.Text));
        }

        // Lista desplegable obligatoria
        public Validador Agregar(ComboBox combo, string nombre)
        {
            Agregar(combo, () => combo.SelectedIndex < 0 ? "Selecciona " + nombre + "." : null);
            combo.SelectedIndexChanged += (s, e) => { if (AlSalir) Validar(combo); };
            return this;
        }

        // Rango de fechas: desde no puede ser mayor que hasta ni haber mas de 5 anos de diferencia
        public Validador AgregarRango(DateTimePicker desde, DateTimePicker hasta)
        {
            Agregar(hasta, () =>
            {
                if (desde.Value.Date > hasta.Value.Date) return "La fecha 'Hasta' no puede ser anterior a 'Desde'.";
                if ((hasta.Value.Date - desde.Value.Date).TotalDays > 366 * 5) return "El rango no puede superar 5 años.";
                return null;
            });
            desde.ValueChanged += (s, e) => Validar(hasta);
            hasta.ValueChanged += (s, e) => Validar(hasta);
            return this;
        }

        // Regla libre sobre cualquier control (devuelve el mensaje o null)
        public Validador Agregar(Control control, Func<string> regla)
        {
            reglas.Add(new KeyValuePair<Control, Func<string>>(control, regla));
            control.Validating += (s, e) => { if (AlSalir) Validar(control); };
            return this;
        }

        // Valida un solo control y muestra u oculta el icono de error
        public bool Validar(Control control)
        {
            string mensaje = null;
            foreach (KeyValuePair<Control, Func<string>> r in reglas)
                if (r.Key == control && (mensaje = r.Value()) != null) break;

            errores.SetError(control, mensaje ?? "");
            return mensaje == null;
        }

        // Valida todos los controles sin detenerse en el primero que falle
        public bool ValidarTodo()
        {
            bool ok = true;
            Control primero = null;
            foreach (Control c in reglas.Select(r => r.Key).Distinct())
                if (c.Enabled && !Validar(c))
                {
                    ok = false;
                    if (primero == null || c.TabIndex < primero.TabIndex) primero = c;
                }
            if (primero != null) primero.Focus();
            return ok;
        }

        // Marca un error que viene de otra validacion (por ejemplo, un duplicado)
        public void Marcar(Control control, string mensaje)
        {
            errores.SetError(control, mensaje);
        }

        public void Limpiar()
        {
            foreach (Control c in reglas.Select(r => r.Key).Distinct())
                errores.SetError(c, "");
        }

        // Restringe lo que se puede escribir en una caja de texto
        public static void Filtrar(TextBox caja, Filtro filtro)
        {
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
