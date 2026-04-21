using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Strategies
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void ExecuteBackup(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string sourceFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, sourceFile);
                string destFile = Path.Combine(target, relativePath);

                string destDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                if (ShouldCopy(sourceFile, destFile))
                    File.Copy(sourceFile, destFile, overwrite: true);
            }
        }

        /// Returns true if the file is new or has been modified since last backup.
        private bool ShouldCopy(string sourceFile, string destFile)
        {
            if (!File.Exists(destFile))
                return true;

            return File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile);
        }
    }
}
