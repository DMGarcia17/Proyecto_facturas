using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Proyecto_facturas.Configuracion
{
    // Lee el archivo .env (CLAVE=valor). Prioridad: variable de entorno > .env > valor por defecto.
    internal static class EnvLoader
    {
        private static readonly Dictionary<string, string> valores =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static bool cargado;

        // Busca el .env en la carpeta del ejecutable y hacia arriba (bin\Debug\net48 -> raiz del proyecto)
        public static void Cargar()
        {
            if (cargado) return;
            cargado = true;

            string ruta = Buscar(AppDomain.CurrentDomain.BaseDirectory) ?? Buscar(Directory.GetCurrentDirectory());
            if (ruta == null) return;

            foreach (string linea in File.ReadAllLines(ruta))
            {
                string t = linea.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;

                int i = t.IndexOf('=');
                if (i <= 0) continue;

                string clave = t.Substring(0, i).Trim();
                string valor = t.Substring(i + 1).Trim();
                if (valor.Length >= 2 && (valor[0] == '"' && valor[valor.Length - 1] == '"'
                    || valor[0] == '\'' && valor[valor.Length - 1] == '\''))
                    valor = valor.Substring(1, valor.Length - 2);

                valores[clave] = valor;
            }
        }

        private static string Buscar(string carpeta)
        {
            DirectoryInfo dir = new DirectoryInfo(carpeta);
            for (int nivel = 0; dir != null && nivel < 6; nivel++, dir = dir.Parent)
            {
                string candidato = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidato)) return candidato;
            }
            return null;
        }

        public static string Obtener(string clave, string porDefecto = null)
        {
            Cargar();
            string entorno = Environment.GetEnvironmentVariable(clave);
            if (!string.IsNullOrEmpty(entorno)) return entorno;
            return valores.TryGetValue(clave, out string v) && v.Length > 0 ? v : porDefecto;
        }

        // Obtiene un valor obligatorio; si falta lanza un mensaje claro
        public static string Requerido(string clave)
        {
            string v = Obtener(clave);
            if (v == null)
                throw new InvalidOperationException("Falta la variable '" + clave + "' en el archivo .env.\n" +
                    "Copia .env.example como .env y completa tus datos.");
            return v;
        }

        public static double ObtenerDecimal(string clave, double porDefecto)
        {
            return double.TryParse(Obtener(clave), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : porDefecto;
        }
    }
}
