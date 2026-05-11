using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Strategies
{

    /// Contract that every backup strategy must implement.
    /// This is the Strategy Pattern — BackupJob works with any
    /// strategy without knowing its details.
    public interface IBackupStrategy
    {
        Task ExecuteBackupAsync(string source, string target, BackupJob job);
        
    }
}

