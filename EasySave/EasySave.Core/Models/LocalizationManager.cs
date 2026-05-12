using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EasySave.Models
{
    public sealed class LocalizationManager
    {
        // 1. Singleton Setup
        private static readonly LocalizationManager _instance = new LocalizationManager();
        public static LocalizationManager Instance => _instance;

        // 2. State variables
        public string CurrentLanguage { get; private set; }
        private Dictionary<string, string> _translations;

        // Private constructor ensures no one else can instantiate this
        private LocalizationManager()
        {
            _translations = new Dictionary<string, string>();
            // Default language is English
            SetLanguage("EN");
        }

        /// <summary>
        /// Loads the translation file for the specified language.
        /// </summary>
        /// <param name="langCode">"EN", "FR", "ES" or "AR"</param>
        public void SetLanguage(string langCode)
        {
            langCode = langCode.ToUpper();
            if (langCode != "EN" && langCode != "FR" && langCode != "ES" && langCode != "AR") return;

            // Prefer loading from the executable output folder (e.g. bin/..../Resources/..).
            // Fallback to legacy relative path for backward compatibility.
            string langLower = langCode.ToLower();
            string filePath = Path.Combine(AppContext.BaseDirectory, "Resources", $"string-{langLower}.json");
            if (!File.Exists(filePath))
            {
                string legacyPath = Path.Combine("Resources", $"string-{langLower}.json");
                filePath = legacyPath;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    CurrentLanguage = langCode;
                }
            }
            catch (Exception ex)
            {
                // Fallback in case of error (e.g., file missing)
                Console.WriteLine($"Error loading language file: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the translated string for a given key.
        /// </summary>
        public string GetString(string key)
        {
            if (_translations.ContainsKey(key))
            {
                return _translations[key];
            }
            // Return the key itself as a fallback if the translation is missing
            return $"[{key}]";
        }
    }
}
