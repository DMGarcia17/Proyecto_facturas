using System;
using System.Security.Cryptography;

namespace Proyecto_facturas.Utilidades
{
    // Hash de contrasenas con PBKDF2 (SHA256) y sal aleatoria. Nunca se guarda la clave en claro.
    internal static class Seguridad
    {
        private const int Iteraciones = 100000;
        private const int BytesSal = 16;
        private const int BytesHash = 32;

        public static void CrearHash(string contrasena, out string hash, out string sal)
        {
            byte[] salBytes = new byte[BytesSal];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salBytes);

            sal = Convert.ToBase64String(salBytes);
            hash = Convert.ToBase64String(Derivar(contrasena, salBytes));
        }

        public static bool Verificar(string contrasena, string hash, string sal)
        {
            byte[] esperado, calculado;
            try
            {
                esperado = Convert.FromBase64String(hash);
                calculado = Derivar(contrasena, Convert.FromBase64String(sal));
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                return false;
            }

            // Comparacion en tiempo constante
            int diferencia = esperado.Length ^ calculado.Length;
            for (int i = 0; i < esperado.Length && i < calculado.Length; i++)
                diferencia |= esperado[i] ^ calculado[i];
            return diferencia == 0;
        }

        private static byte[] Derivar(string contrasena, byte[] sal)
        {
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256))
                return kdf.GetBytes(BytesHash);
        }
    }
}
