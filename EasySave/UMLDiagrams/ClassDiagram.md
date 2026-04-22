```classDiagram
    %% ==============================
    %% VIEW LAYER
    %% ==============================
    class ConsoleView {
        -MainViewModel _viewModel
        +ConsoleView(MainViewModel vm)
        +DisplayMenu()
        -CreateJobMenu()
        -ExecuteSingleJobMenu()
        -DisplayJobsMenu()
        -DeleteJobMenu()
        -ModifyJobMenu()
        -ChangeLanguageMenu()
        -ReadUserInput() string
    }

    %% ==============================
    %% VIEWMODEL LAYER
    %% ==============================
    class MainViewModel {
        -BackupManager _backupManager
        -IConfigManager _configManager
        -StateManager _stateManager
        -SettingsManager _settingsManager
        -AppSettings _currentSettings
        +List~BackupJob~ Jobs
        +MainViewModel(IConfigManager configManager)
        +CreateJobCommand(name, source, target, isDifferential) bool
        +DeleteJobCommand(jobId) bool
        +ModifyJobCommand(jobId, name, source, target, isDifferential) bool
        +ExecuteJobCommand(jobId)
        +ExecuteAllJobsCommand()
        +ChangeLanguageCommand(langCode)
        +GetString(key) string
    }

    %% ==============================
    %% MODEL LAYER - CORE
    %% ==============================
    class BackupManager {
        -List~BackupJob~ _jobs
        -IConfigManager _configManager
        -StateManager _stateManager
        +BackupManager(IConfigManager configManager)
        +CreateJob(name, source, target, isDifferential) bool
        +DeleteJob(index) bool
        +ModifyJob(index, newName, newSource, newTarget, isDifferential) bool
        +ExecuteJob(index)
        +ExecuteAll()
        +GetJobs() List~BackupJob~
        -LoadFromConfig()
    }

    class BackupJob {
        +string Name
        +string SourcePath
        +string TargetPath
        +JobState State
        +int TotalFiles
        +long TotalSize
        +int FilesRemaining
        +long SizeRemaining
        +string CurrentSourceFile
        +string CurrentTargetFile
        -IBackupStrategy _strategy
        +BackupJob(name, source, target, strategy)
        +Execute()
        +GetStrategy() IBackupStrategy
        +NotifyProgress()
    }

    class JobState {
        <<enumeration>>
        Inactive
        Active
        Paused
        Completed
        Error
    }

    %% ==============================
    %% MODEL LAYER - STRATEGIES
    %% ==============================
    class IBackupStrategy {
        <<interface>>
        +ExecuteBackup(sourceDir, targetDir, jobContext)
    }
    class FullBackupStrategy {
        +ExecuteBackup(sourceDir, targetDir, jobContext)
    }
    class DifferentialBackupStrategy {
        +ExecuteBackup(sourceDir, targetDir, jobContext)
    }

    %% ==============================
    %% MODEL LAYER - MANAGERS & DTOs
    %% ==============================
    class IConfigManager {
        <<interface>>
        +SaveJobs(List~BackupJob~ jobs)
        +LoadJobs() List~JobSaveData~
    }
    class ConfigManager {
        -_configFilePath : string
        +SaveJobs(jobs)
        +LoadJobs() List~JobSaveData~
    }
    class StateManager {
        -_stateFilePath : string
        -_jobStates : Dictionary
        +OnJobProgressUpdated(sender, e)
        -UpdateStateFile(job)
        -LoadExistingStates()
    }
    class LocalizationManager {
        <<Singleton>>
        -static _instance : LocalizationManager
        +CurrentLanguage : string
        -LocalizationManager()
        +Instance : LocalizationManager
        +SetLanguage(langCode)
        +GetString(key) string
    }

    %% ==============================
    %% EXTERNAL LIBRARY (DLL)
    %% ==============================
    namespace EasyLog {
        class EasyLogger {
            <<Singleton>>
            -static Lazy~EasyLogger~ LazyInstance
            -_logDirectory : string
            -_writeLock : object
            -EasyLogger()
            +Instance : EasyLogger
            +WriteLog(LogEntry entry)
        }
        class LogEntry {
            +DateTime Timestamp
            +string BackupName
            +string SourceFilePath
            +string TargetFilePath
            +long FileSize
            +long TransferTimeMs
        }
    }

    %% ==============================
    %% RELATIONSHIPS
    %% ==============================
    ConsoleView --> MainViewModel : Binds to
    MainViewModel --> BackupManager : Invokes commands
    MainViewModel --> IConfigManager : Injects
    MainViewModel --> LocalizationManager : Fetches strings
    
    BackupManager *-- BackupJob : Manages up to 5
    BackupManager --> IConfigManager : Uses for persistence
    BackupManager --> StateManager : Registers events
    
    BackupJob --> JobState : Has state
    BackupJob o-- IBackupStrategy : Uses pattern
    BackupJob ..> EasyLogger : Logs transfers (via strategy)
    
    IBackupStrategy <|.. FullBackupStrategy : Implements
    IBackupStrategy <|.. DifferentialBackupStrategy : Implements
    
    IConfigManager <|.. ConfigManager : Implements
```