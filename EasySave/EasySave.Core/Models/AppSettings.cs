using System.Collections.Generic;

namespace EasySave.Models
{
    // Represents the global application settings saved in appsettings.json
    public class AppSettings
    {
        // The preferred language code (e.g., "EN" or "FR"). Defaults to English.
        public string Language { get; set; } = "EN";

        // The preferred log format for Version 1.1 (e.g., "JSON" or "XML")
        public string LogFormat { get; set; } = "JSON";

        // Preferred visual mode for the UI ("Claire" or "Sombre").
        public string ThemeMode { get; set; } = "Claire";

        // List of file extensions that should be encrypted by CryptoSoft for Version 2.0
        public List<string> EncryptedExtensions { get; set; } = new List<string> { ".txt", ".docx", ".pdf" };

        public string CryptoSoftPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave\\CryptoSoft\\bin\\Debug\\net8.0\\win-x64\\CryptoSoft.exe");

        public string EncryptionKey { get; set; } = "default";

        // The name of the business software that blocks backups for Version 2.0
        public string BusinessSoftwareName { get; set; } = "calculator";
    }
}