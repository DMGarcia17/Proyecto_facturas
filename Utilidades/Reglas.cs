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
                if (v.Length > 150 || !PatronEmail.IsMatch(v)) return "Correo inválido (ej. nombre@dominio.com).";
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
                if (!caracteres || digitos < 8 || digitos > 15) return "Teléfono inválido (8 a 15 dígitos).";
                return null;
            };
        }

        public static Func<string, string> Usuario()
        {
            return t => PatronUsuario.IsMatch((t ?? "").Trim())
                ? null
                : "Usa 3 a 50 letras, números, . - _";
        }

        // De 8 a 64 caracteres, con al menos una letra y un numero
        public static Func<string, string> Contrasena()
        {
            return t =>
            {
                string v = t ?? "";
                if (v.Length < 8 || v.Length > 64 || !v.Any(char.IsLetter) || !v.Any(char.IsDigit))
                    return "Mín. 8 caracteres, con letras y números.";
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
                if (!EsEntero(t, out int v)) return "Escribe un número entero.";
                int tope = max();
                if (v < min) return "Mínimo permitido: " + min.ToString("N0") + ".";
                if (v > tope) return tope <= 0 ? "No hay existencias." : "Máximo disponible: " + tope.ToString("N0") + ".";
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
                    return "Debe ser mayor a cero (máx. 2 decimales).";
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

}
