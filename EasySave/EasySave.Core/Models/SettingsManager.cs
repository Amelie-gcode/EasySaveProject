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
                settings.EncryptionKey = SecurityHelper.Protect(settings.EncryptionKey);
                string jsonContent = JsonSerializer.Serialize(settings, options);
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