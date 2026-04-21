```mermaid
classDiagram
    %% --- MAIN ENTRY POINT ---
    namespace EasySave_Main {
        class Program {
            + static Main(string[] args)
        }
    }

    %% --- VIEWS (MVVM) ---
    namespace EasySave_Views {
        class ConsoleView {
            - MainViewModel _viewModel
            + ConsoleView(MainViewModel vm)
            + DisplayMenu()
            + ReadUserInput()
        }
    }

    %% --- VIEWMODELS (MVVM) ---
    namespace EasySave_ViewModels {
        class MainViewModel {
            - BackupManager _backupManager
            - LocalizationManager _localization
            + string CurrentLanguage
            + List~BackupJob~ Jobs
            + ExecuteJobCommand(int id)
            + ExecuteJobsFromCommandLine(string arguments)
            + ChangeLanguageCommand(string langCode)
        }
    }

    %% --- MODELS (CORE BUSINESS LOGIC) ---
    namespace EasySave_Models {
        class JobState {
            <<enumeration>>
            Inactive
            Active
            Completed
            Error
        }

        class BackupManager {
            - List~BackupJob~ _jobs
            - StateManager _stateManager
            + CreateJob(name, source, target, type)
            + ExecuteJob(int id)
            + ExecuteAll()
        }

        class BackupJob {
            + string Name
            + string SourcePath
            + string TargetPath
            + JobState State
            - IBackupStrategy _strategy
            + event EventHandler ProgressUpdated
            + Execute()
            - NotifyProgress()
        }

        class StateManager {
            - string _stateFilePath
            + OnJobProgressUpdated(sender, args)
            - WriteToJson(data)
        }

        class LocalizationManager {
            <<Singleton>>
            - string _currentLanguage
            - Dictionary _translations
            + static Instance
            + SetLanguage(string langCode)
            + GetString(string key) : string
        }

        class IBackupStrategy {
            <<Interface>>
            + ExecuteBackup(string source, string target)
        }
        
        class FullBackupStrategy {
            + ExecuteBackup(string source, string target)
        }
        
        class DifferentialBackupStrategy {
            + ExecuteBackup(string source, string target)
        }
    }

    %% --- REQUIRED DLL ---
    namespace EasyLog_DLL {
        class EasyLogger {
            <<Singleton>>
            - string _logDirectory
            + static Instance
            + WriteLog(logEntry)
        }
    }

    %% --- UNIT TESTING ---
    namespace EasySave_Tests {
        class BackupJobTests {
            + Test_FullBackup_Success()
            + Test_DifferentialBackup_IgnoresUnchanged()
        }
        class LoggerTests {
            + Test_SingletonInstance()
        }
    }

    %% --- RELATIONSHIPS & DEPENDENCIES ---
    
    %% Bootstrapping
    Program --> ConsoleView : Instantiates & Starts
    Program --> MainViewModel : Instantiates
    
    %% MVVM Bindings
    ConsoleView --> MainViewModel : Binds to / Interacts
    MainViewModel --> BackupManager : Invokes commands
    MainViewModel --> LocalizationManager : Requests text
    MainViewModel ..> BackupJob : Observes progress (via Events)
    
    %% Core Model Composition
    BackupManager "1" *-- "1" StateManager : Instantiates
    BackupManager "1" *-- "0..5" BackupJob : Manages
    
    %% Strategy Pattern
    BackupJob o-- IBackupStrategy : Uses
    IBackupStrategy <|.. FullBackupStrategy : Implements
    IBackupStrategy <|.. DifferentialBackupStrategy : Implements
    
    %% State Tracking & Observer Pattern
    BackupJob --> JobState : Uses
    BackupJob ..> StateManager : Triggers event (decoupled)
    
    %% Logging (Singleton)
    BackupJob ..> EasyLogger : Sends log data
```