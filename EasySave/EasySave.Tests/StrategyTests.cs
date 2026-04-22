using EasySave.Models;
using EasySave.Strategies;
using System;
using System.IO;
using Xunit;

namespace EasySave.Tests
{
    // IDisposable ensures the Dispose() method is called after the test runs
    public class StrategyTests : IDisposable
    {
        private readonly string _sourceDir;
        private readonly string _targetDir;

        public StrategyTests()
        {
            // Arrange: Create unique temporary directories for each test run
            _sourceDir = Path.Combine(Path.GetTempPath(), "EasySave_Test_Source");
            _targetDir = Path.Combine(Path.GetTempPath(), "EasySave_Test_Target");

            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_targetDir);
        }

        [Fact]
        public void FullBackupStrategy_ShouldCopyAllFilesToTarget()
        {
            // Arrange: Create a fake file in the source directory
            string testFileName = "test_document.txt";
            string sourceFilePath = Path.Combine(_sourceDir, testFileName);
            string expectedTargetFilePath = Path.Combine(_targetDir, testFileName);

            File.WriteAllText(sourceFilePath, "Hello, ProSoft!");

            var strategy = new FullBackupStrategy();
            var dummyJob = new BackupJob("TestJob", _sourceDir, _targetDir, strategy);

            // Act: Execute the backup strategy
            strategy.ExecuteBackup(_sourceDir, _targetDir, dummyJob);

            // Assert: Verify the file was copied and the content matches
            Assert.True(File.Exists(expectedTargetFilePath), "The file should exist in the target directory.");
            Assert.Equal("Hello, ProSoft!", File.ReadAllText(expectedTargetFilePath));
        }

        public void Dispose()
        {
            // Cleanup: Delete the temporary directories and their contents
            if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, true);
            if (Directory.Exists(_targetDir)) Directory.Delete(_targetDir, true);
        }
    }
}
