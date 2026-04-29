using System.Security.Cryptography;
using System.Text;

public static class SecurityHelper
{
    // Chiffre la clé pour le stockage
    public static string Protect(string clearText)
    {
        byte[] data = Encoding.UTF8.GetBytes(clearText);
        byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    // Déchiffre la clé pour l'utiliser avec CryptoSoft
    public static string Unprotect(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}