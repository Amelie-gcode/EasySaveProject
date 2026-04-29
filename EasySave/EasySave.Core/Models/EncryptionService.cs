using System.Diagnostics;

public class EncryptionService
{
    private readonly string _cryptoSoftPath;
    private readonly string[] _targetExtensions;

    public EncryptionService(string exePath, string[] extensions)
    {
        _cryptoSoftPath = exePath;
        _targetExtensions = extensions;
    }

    public bool ShouldEncrypt(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return _targetExtensions.Contains(extension);
    }

    public int Encrypt(string source, string target, string key)
    {
        // On ne lance le processus que si le fichier doit être crypté
        if (!ShouldEncrypt(source)) return 0;

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = _cryptoSoftPath,
            Arguments = $"\"{source}\" \"{target}\" \"{key}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        using (Process p = Process.Start(startInfo))
        {
            p.WaitForExit();
            return p.ExitCode; // CryptoSoft peut retourner le temps de cryptage ou un code d'erreur
        }
    }
}