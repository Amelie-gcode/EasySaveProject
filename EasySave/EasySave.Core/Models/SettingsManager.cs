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


                // Do not mutate the in-memory object
                var normalizedKey = NormalizeEncryptionKey(settings.EncryptionKey);
                var settingsToSave = new AppSettings
                {
                    Language = settings.Language,
                    LogFormat = settings.LogFormat,
                    LogDestination = settings.LogDestination,
                    LogCentralizerUrl = settings.LogCentralizerUrl,
                    ThemeMode = settings.ThemeMode,
                    EncryptedExtensions = settings.EncryptedExtensions ?? new List<string>(),
                    CryptoSoftPath = NormalizeCryptoSoftPath(settings.CryptoSoftPath),
                    EncryptionKey = string.IsNullOrWhiteSpace(normalizedKey) ? "default" : normalizedKey,
                    BusinessSoftwareName = settings.BusinessSoftwareName ?? new List<string>(),
                    PriorityExtensions = settings.PriorityExtensions ?? new List<string>()
                };

                string jsonContent = JsonSerializer.Serialize(settingsToSave, options);

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
            // Use the default path from AppSettings if not configured
            string pathToResolve = string.IsNullOrWhiteSpace(configuredPath)
                ? new AppSettings().CryptoSoftPath
                : configuredPath;

            // If the path is relative, make it absolute using the application base directory
            if (!Path.IsPathRooted(pathToResolve))
            {
                pathToResolve = Path.Combine(AppContext.BaseDirectory, pathToResolve);
            }

            return pathToResolve;
        }

        private static string NormalizeCryptoSoftPath(string? absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return new AppSettings().CryptoSoftPath;
            }

            // If the path is absolute, try to convert it to relative
            if (Path.IsPathRooted(absolutePath))
            {
                try
                {
                    string basePath = AppContext.BaseDirectory;
                    if (absolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove the base directory to get the relative path
                        string relativePath = absolutePath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        return relativePath;
                    }
                }
                catch
                {
                    // If conversion fails, return the absolute path as-is
                }
            }

            return absolutePath;
        }

        private static string NormalizeEncryptionKey(string? rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return "default";
            }

            return rawKey.Trim();
        }
    }
}