using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LabControl.Client.Kiosk.Services;

/// <summary>
/// Provee cifrado y descifrado robusto (AES-256) para proteger la contraseña maestra
/// de soporte técnico almacenada localmente en kiosk-config.json.
/// Evita que estudiantes curiosos lean la clave en texto plano inspeccionando los archivos de configuración.
/// </summary>
public static class PasswordEncryptionHelper
{
    private const string EncPrefix = "ENC:";

    // Clave institucional derivada de 256 bits para el ecosistema LabControl Kiosk
    private static readonly byte[] AesKey = SHA256.HashData(Encoding.UTF8.GetBytes("UnivalleLabControl_Kiosk_SecretKey_2026#SecureCampusKey"));
    private static readonly byte[] AesIv = MD5.HashData(Encoding.UTF8.GetBytes("UnivalleLabControl_Kiosk_IV_2026#Vector"));

    /// <summary>
    /// Cifra una contraseña en texto plano produciendo una cadena Base64 con prefijo "ENC:".
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        if (IsEncrypted(plainText)) return plainText; // Ya se encuentra cifrado

        try
        {
            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            var encryptedBytes = ms.ToArray();
            return EncPrefix + Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            // Fallback
            return plainText;
        }
    }

    /// <summary>
    /// Descifra una contraseña que comienza con "ENC:". Si está en texto plano, la devuelve intacta.
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        if (!IsEncrypted(cipherText)) return cipherText; // Formato heredado o texto plano

        try
        {
            var base64 = cipherText.Substring(EncPrefix.Length);
            var buffer = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        catch
        {
            return cipherText;
        }
    }

    public static bool IsEncrypted(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && text.StartsWith(EncPrefix, StringComparison.Ordinal);
    }
}
