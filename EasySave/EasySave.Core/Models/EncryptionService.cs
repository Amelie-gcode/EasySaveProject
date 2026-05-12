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


    public long Encrypt(string target, string key)
    {
        LastError = string.Empty;

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(target) || !File.Exists(target))
        {

            LastError = "Invalid encryption input (key/target).";
            return -1;
        }

        if (string.IsNullOrWhiteSpace(_cryptoSoftPath) || !File.Exists(_cryptoSoftPath))
        {
            LastError = $"CryptoSoft executable not found at '{_cryptoSoftPath}'.";
            return -1;
        }

        try
        {
            string workingDirectory = Path.GetDirectoryName(_cryptoSoftPath) ?? AppContext.BaseDirectory;
            string args = $"\"{target}\" \"{key}\"";

            // Attempt 1: standard non-shell execution (preferred)
            var startInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };
            long firstAttempt = RunProcess(startInfo);
            if (firstAttempt >= 0) return firstAttempt;

            // Attempt 2: shell execution (some external tools require this)
            var shellStartInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = args,
                UseShellExecute = true,
                WorkingDirectory = workingDirectory
            };
            long secondAttempt = RunProcess(shellStartInfo);
            if (secondAttempt >= 0) return secondAttempt;

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            LastError = ex.Message;
            return -1;
        }
    }
}