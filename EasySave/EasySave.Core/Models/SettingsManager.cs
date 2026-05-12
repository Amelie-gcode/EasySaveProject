using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Models
{
    public class SettingsManager
    {
        private readonly string _settingsFilePath;

        public SettingsManager()
        {
            // Locate the AppData/Roaming folder
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "EasySave");

            // Ensure the directory exists
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _settingsFilePath = Path.Combine(directory, "appsettings.json");
        }
        // Constructor with a custom path for testing purposes
        public SettingsManager(string customPath)
        {
            _settingsFilePath = customPath;
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Return default settings if no file is found
                var defaultSettings = new AppSettings();
                defaultSettings.CryptoSoftPath = ResolveCryptoSoftPath(defaultSettings.CryptoSoftPath);
                return defaultSettings;
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                AppSettings loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

                if (loadedSettings != null)
                {
                    // Ensure key is fully decrypted even if it was accidentally protected multiple times.
                    loadedSettings.EncryptionKey = NormalizeEncryptionKey(loadedSettings.EncryptionKey);
                    loadedSettings.CryptoSoftPath = ResolveCryptoSoftPath(loadedSettings.CryptoSoftPath);
                }

                return loadedSettings ?? new AppSettings { CryptoSoftPath = ResolveCryptoSoftPath(null) };
            }
            catch
            {
                var fallbackSettings = new AppSettings();
                fallbackSettings.CryptoSoftPath = ResolveCryptoSoftPath(fallbackSettings.CryptoSoftPath);
                return fallbackSettings;
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
<<<<<<< feature/CryptoSoft

                // Do not mutate the in-memory object, otherwise the key can be re-protected repeatedly.
                var normalizedKey = NormalizeEncryptionKey(settings.EncryptionKey);
                var settingsToSave = new AppSettings
                {
                    Language = settings.Language,
                    LogFormat = settings.LogFormat,
                    ThemeMode = settings.ThemeMode,
                    EncryptedExtensions = settings.EncryptedExtensions ?? new List<string>(),
                    CryptoSoftPath = settings.CryptoSoftPath,
                    EncryptionKey = SecurityHelper.Protect(string.IsNullOrWhiteSpace(normalizedKey) ? "default" : normalizedKey),
                    BusinessSoftwareName = settings.BusinessSoftwareName ?? new List<string>(),
                    PriorityExtensions = settings.PriorityExtensions ?? new List<string>()
                };

                string jsonContent = JsonSerializer.Serialize(settingsToSave, options);
=======
                settings.EncryptionKey = SecurityHelper.Protect(settings.EncryptionKey);
                string jsonContent = JsonSerializer.Serialize(settings, options);
>>>>>>> develop
                File.WriteAllText(_settingsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // In a production environment, you would log this to a debug file
                Console.WriteLine($"Warning: Could not save settings. {ex.Message}");
            }
        }

        private static string ResolveCryptoSoftPath(string? configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                return configuredPath;
            }

            var detectedPath = FindCryptoSoftInProjectHierarchy();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                return detectedPath;
            }

            return string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(AppContext.BaseDirectory, "CryptoSoft.exe")
                : configuredPath;
        }

        private static string? FindCryptoSoftInProjectHierarchy()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var directPath = Path.Combine(current.FullName, "CryptoSoft.exe");
                if (File.Exists(directPath))
                {
                    return directPath;
                }

                var subFolderPath = Path.Combine(current.FullName, "CryptoSoft", "CryptoSoft.exe");
                if (File.Exists(subFolderPath))
                {
                    return subFolderPath;
                }

                current = current.Parent;
            }

            return null;
        }

        private static string NormalizeEncryptionKey(string? rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return "default";
            }

            string current = rawKey;

            // Handles accidental multiple DPAPI protections by unwrapping layers.
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    string next = SecurityHelper.Unprotect(current);
                    if (string.Equals(next, current, StringComparison.Ordinal))
                    {
                        break;
                    }
                    current = next;
                }
                catch
                {
                    break;
                }
            }

            return string.IsNullOrWhiteSpace(current) ? "default" : current;
        }
    }
}