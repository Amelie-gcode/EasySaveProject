using System;
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
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                AppSettings loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);

                if (loadedSettings != null)
                {
                    // On déchiffre la clé pour qu'elle soit utilisable par le programme
                    loadedSettings.EncryptionKey = SecurityHelper.Unprotect(loadedSettings.EncryptionKey);
                }

                return loadedSettings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };


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

                File.WriteAllText(_settingsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // In a production environment, you would log this to a debug file
                Console.WriteLine($"Warning: Could not save settings. {ex.Message}");
            }
        }
    }
}