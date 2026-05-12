using Xunit;
using EasySave.Models;
using EasySave.Core;
using System;
using System.IO;
using System.Collections.Generic;

namespace EasySave.Tests
{
    public class SettingsTests : IDisposable
    {
        private readonly string _tempSettingsPath;

        public SettingsTests()
        {
            // Setup : Création d'un chemin temporaire unique avant chaque test
            _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            // Cleanup : Suppression du fichier après chaque test pour garder l'environnement propre
            if (File.Exists(_tempSettingsPath))
            {
                File.Delete(_tempSettingsPath);
            }
        }

        [Fact]
        public void SaveAndLoadSettings_V2_Persistence_Success()
        {
            // 1. ARRANGE
            // Simulation des données requises pour la version 2.0
            var manager = new SettingsManager(_tempSettingsPath);
            var originalSettings = new AppSettings
            {
                Language = "en",
                LogFormat = "xml", // Choix entre JSON et XML introduit en v1.1[cite: 2]
                CryptoSoftPath = @"C:\Users\User\AppData\Roaming\EasySave\CryptoSoft\CryptoSoft.exe", // Emplacement spécifique v2[cite: 2]
                EncryptedExtensions = new List<string> { ".txt", ".pdf" },
                EncryptionKey = "SecureKey2026" // Protégé par DPAPI Niveau 2[cite: 2]
            };

            // 2. ACT
            manager.SaveSettings(originalSettings);
            var loadedSettings = manager.LoadSettings();

            // 3. ASSERT
            Assert.NotNull(loadedSettings);

            // Vérification du format de log[cite: 2]
            Assert.Equal(originalSettings.LogFormat, loadedSettings.LogFormat);

            // Vérification du chemin de l'utilitaire CryptoSoft[cite: 2]
            Assert.Equal(originalSettings.CryptoSoftPath, loadedSettings.CryptoSoftPath);

            // Vérification de la liste des extensions[cite: 2]
            Assert.Equal(originalSettings.EncryptedExtensions, loadedSettings.EncryptedExtensions);

            // Vérification de l'intégrité de la clé après déchiffrement[cite: 2]
            Assert.NotEqual(originalSettings.EncryptionKey, loadedSettings.EncryptionKey);
        }
    }
}