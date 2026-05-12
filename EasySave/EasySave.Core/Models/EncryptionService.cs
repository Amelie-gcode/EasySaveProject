using System.Diagnostics;

public class EncryptionService
{
    private readonly string _cryptoSoftPath;
    private readonly HashSet<string> _targetExtensions;
    public string LastError { get; private set; } = string.Empty;

    public EncryptionService(string exePath, string[] extensions)
    {
        _cryptoSoftPath = exePath;
        _targetExtensions = new HashSet<string>(
            (extensions ?? Array.Empty<string>())
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .Select(ext => ext.StartsWith(".") ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool ShouldEncrypt(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
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

            // Attempt 3: cmd fallback
            var cmdStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{_cryptoSoftPath}\" {args}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };
            long thirdAttempt = RunProcess(cmdStartInfo);
            if (thirdAttempt >= 0) return thirdAttempt;

            if (string.IsNullOrWhiteSpace(LastError))
                LastError = "CryptoSoft failed to start.";
            return -1;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return -1;
        }
    }

    private long RunProcess(ProcessStartInfo startInfo)
    {
        try
        {
            using Process? p = Process.Start(startInfo);
            if (p == null)
            {
                LastError = "Process.Start returned null.";
                return -1;
            }

            p.WaitForExit();
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return -1;
        }
    }
}