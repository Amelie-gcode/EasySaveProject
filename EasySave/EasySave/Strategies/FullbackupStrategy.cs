using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EasySave.Strategies
{
        /// Copies every file from source to target, every time.
        /// Does not check if files already exist or are unchanged.
        public class FullBackupStrategy : IBackupStrategy
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

                    File.Copy(sourceFile, destFile, overwrite: true);
                }
            }
        }
}

