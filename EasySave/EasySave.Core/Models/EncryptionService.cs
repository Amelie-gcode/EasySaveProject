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

    public long Encrypt( string target, string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = _cryptoSoftPath,
            Arguments = $"\"{target}\" \"{key}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };


        using (Process p = Process.Start(startInfo))
        {
            if (p == null) return -1;

            p.WaitForExit();

            // Capture the ElapsedMilliseconds returned by CryptoSoft's Environment.Exit()
            return (long)p.ExitCode;
        }
    }
}