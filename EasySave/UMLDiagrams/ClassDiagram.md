```mermaid
classDiagram
    %% =====================================================
    %% EASYSAVE NAMESPACE (.NET Framework 4.7.2)
    %% =====================================================
    
    namespace EasyLog {
        %% Enums and Data Models
        class LogEntry {
            +DateTime Timestamp
            +string BackupName
            +string SourceFilePath
            +string TargetFilePath
            +long FileSize
            +long TransferTimeMs
            +long? EncryptionTimeMs
        }
        
        %% Interfaces
        class ILogWriter {
            <<interface>>
            +WriteLog(LogEntry entry) void
        }
        
        %% Logger Singleton
        class EasyLogger {
            -static EasyLogger _instance
            -string _logsFolderPath
            -object _fileLock
            +static Instance$ EasyLogger
            +WriteLog(LogEntry entry) void
            -GetDailyLogPath() string
            -BuildJsonEntry(LogEntry entry) string
            -EnsureDirectoryExists() void
        }
        
        %% Implementations
        class JsonLogWriter {
            +WriteLog(LogEntry entry) void
        }
        
        class XMLLogWriter {
            +WriteLog(LogEntry entry) void
        }
    }
    
    %% =====================================================
    %% EASYSAVE.CORE NAMESPACE (.NET 10)
    %% =====================================================
    
    namespace EasySave_Core_Models {
        %% Enums
        class JobState {
            <<enumeration>>
            Inactive
            Active
            Paused
            Completed
            Cancelled
            Error
        }
        
        %% Data Models
        class JobSaveData {
            +string Name
            +string SourcePath
            +string TargetPath
            +bool IsDifferential
        }
        
        class JobStateData {
            +string JobName
            +JobState State
            +int TotalFiles
            +long TotalSize
            +int FilesRemaining
            +long SizeRemaining
            +string CurrentSourceFile
            +string CurrentTargetFile
            +double ProgressPercent
            +DateTime Timestamp
        }
        
        class AppSettings {
            +string Language
            +string LogFormat
            +string EncryptionKey
            +List~string~ EncryptedExtensions
            +List~string~ BusinessSoftwareName
            +string CryptoSoftPath
            +List~string~ PriorityExtensions
            +long MaxParallelSize
        }
        
        %% Configuration Management
        class IConfigManager {
            <<interface>>
            +SaveJobs(List~BackupJob~ jobs) void
            +LoadJobs() List~BackupJob~
        }
        
        class ConfigManager {
            -string _configPath
            +SaveJobs(List~BackupJob~ jobs) void
            +LoadJobs() List~BackupJob~
        }
        
        %% Settings Management
        class SettingsManager {
            -string _settingsPath
            +LoadSettings() AppSettings
            +SaveSettings(AppSettings settings) void
        }
        
        %% State Management
        class StateManager {
            -List~JobStateData~ _history
            -object _fileLock
            -string _statePath
            +OnJobProgressUpdated(object sender, EventArgs e) void
            -UpdateStateFile(BackupJob job) void
            -LoadExistingStates() void
        }
        
        %% Security & Encryption
        class SecurityHelper {
            +EncryptKey(string key) string
            +DecryptKey(string encryptedKey) string
        }
        
        class EncryptionService {
            -string _cryptoSoftPath
            -List~string~ _encryptedExtensions
            +ShouldEncrypt(string filePath) bool
            +Encrypt(string filePath, string key) long
            -ExecuteCryptoSoft(string filePath, string key) long
        }
        
        %% Localization
        class LocalizationManager {
            -static LocalizationManager _instance
            -Dictionary~string, string~ _strings
            -string _currentLanguage
            +static Instance$ LocalizationManager
            +CurrentLanguage$ string
            +SetLanguage(string langCode) void
            +GetString(string key) string
            -LoadLanguage(string langCode) void
        }
        
        %% Core Backup Job
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
            +EncryptionService Encryption
            +string EncryptionKey
            +BusinessSoftwareService BusinessService
            +AppSettings Settings
            +int LocalPriorityFilesCount
            -IBackupStrategy _strategy
            -ManualResetEventSlim _pauseGate
            -int _cancelRequested
            -static int _globalPriorityFilesCount
            +event EventHandler ProgressUpdated
            +BackupJob(string name, string source, string target, IBackupStrategy strategy)
            +Execute() Task
            +RequestPause() void
            +RequestResume() void
            +RequestCancel() void
            +CheckPauseAndCancellation() void
            +NotifyProgress() void
            +GetStrategy() IBackupStrategy
            -CalculateInitialStats() void
            -HandlePathError(string errorMessage) void
            +static OthersHavePriority(int myCount) bool
            +static IncrementGlobalPriority() void
            +static DecrementGlobalPriority() void
        }
        
        %% Backup Manager
        class BackupManager {
            -List~BackupJob~ _jobs
            -IConfigManager _configManager
            -SettingsManager _settingsManager
            -AppSettings _currentSettings
            -EncryptionService _encryptionService
            -BusinessSoftwareService _businessSoftwareService
            -StateManager _stateManager
            +BackupManager()
            +GetJobs() List~BackupJob~
            +CreateJob(string name, string source, string target, bool isDifferential) void
            +DeleteJob(int index) void
            +ModifyJob(int index, string name, string source, string target, bool isDifferential) void
            +ExecuteJob(int jobId) void
            +ExecuteAll() void
            -LoadFromConfig() void
        }
    }
    
    namespace EasySave_Core_Strategies {
        %% Backup Strategy Pattern
        class IBackupStrategy {
            <<interface>>
            +ExecuteBackupAsync(string sourceDir, string targetDir, BackupJob jobContext) Task
        }
        
        class FullBackupStrategy {
            +ExecuteBackupAsync(string sourceDir, string targetDir, BackupJob jobContext) Task
            -CopyFileAsync(string sourceFile, string targetFile, BackupJob jobContext) Task
        }
        
        class DifferentialBackupStrategy {
            +ExecuteBackupAsync(string sourceDir, string targetDir, BackupJob jobContext) Task
            -CopyFileAsync(string sourceFile, string targetFile, BackupJob jobContext) Task
        }
        
        %% Business Software Detection
        class BusinessSoftwareService {
            +IsBusinessSoftwareRunning(List~string~ softwareList) bool
            +GetDetectedSoftwareName(List~string~ softwareList) string
        }
    }
    
    %% =====================================================
    %% EASYSAVE.WPF NAMESPACE (.NET 10 + WPF)
    %% =====================================================
    
    namespace EasySave_WPF {
        %% Command Implementations
        class RelayCommand {
            -Action~object?~ _execute
            -Predicate~object?~ _canExecute
            +event EventHandler CanExecuteChanged
            +RelayCommand(Action~object?~ execute, Predicate~object?~ canExecute)
            +CanExecute(object? parameter) bool
            +Execute(object? parameter) void
            +RaiseCanExecuteChanged() void
        }
        
        class AsyncRelayCommand {
            -Func~Task~ _execute
            -Func~bool~ _canExecute
            +event EventHandler CanExecuteChanged
            +AsyncRelayCommand(Func~Task~ execute, Func~bool~ canExecute)
            +CanExecute(object? parameter) bool
            +Execute(object? parameter) void
            +RaiseCanExecuteChanged() void
        }
        
        %% View Model Layer
        class MainViewModel {
            -BackupManager _backupManager
            -SettingsManager _settingsManager
            -AppSettings _currentSettings
            -Dispatcher _dispatcher
            -bool _isBusy
            -JobViewModel? _selectedJob
            -string _jobName
            -string _sourcePath
            -string _targetPath
            -bool _isDifferential
            -string _selectedLanguage
            -string _selectedLogFormat
            -string _encryptedExtensionsInput
            -string _businessSoftwareInput
            -string _encryptionKeyInput
            -bool _isModifyPanelOpen
            +ObservableCollection~JobViewModel~ Jobs
            +JobViewModel? SelectedJob
            +string SelectedLanguage
            +string SelectedLogFormat
            +string EncryptedExtensionsInput
            +string BusinessSoftwareInput
            +string EncryptionKeyInput
            +bool IsModifyPanelOpen
            +bool IsBusy
            +ICommand ExecuteSelectedJobCommand
            +ICommand ExecuteAllJobsCommand
            +ICommand ExecuteCheckedJobsCommand
            +ICommand CreateJobCommand
            +ICommand DeleteSelectedJobCommand
            +ICommand ModifySelectedJobCommand
            +ICommand BrowseSourceCommand
            +ICommand BrowseTargetCommand
            +ICommand SaveSettingsCommand
            +ICommand DeleteJobCommand
            +ICommand ModifyJobCommand
            +ICommand OpenModifyPanelCommand
            +ICommand CloseModifyPanelCommand
            +event PropertyChangedEventHandler? PropertyChanged
            +MainViewModel()
            -LoadJobs() void
            -RefreshExecutionCanExecute() void
            -HasValidJobInputs() bool
            -CanCreateJob() bool
            -CanModifySelectedJob() bool
            -RefreshCrudCanExecute() void
            -ApplyLanguage(string langCode) void
            -BrowseAndSetPath(bool isSource) void
            -static BrowseFolder(string? initialPath) string?
            -CreateJobAsync() Task
            -DeleteSelectedJobAsync() Task
            -DeleteJobAsync(object? parameter) Task
            -ModifySelectedJobAsync() Task
            -ModifyJobAsync(object? parameter) Task
            -OpenModifyPanel(object? parameter) void
            -SaveSettings() void
            -static ParseCsvList(string? input, bool ensureDotPrefix) List~string~
            -ExecuteSelectedJobAsync() Task
            -ExecuteAllJobsAsync() Task
            -ExecuteCheckedJobsAsync() Task
            -OnPropertyChanged(string? propertyName) void
        }
        
        %% Job View Model (nested in MainViewModel)
        class JobViewModel {
            +int Id
            -BackupJob _job
            -Dispatcher _dispatcher
            -EventHandler _progressHandler
            -Action _onCheckedChanged
            -bool _isChecked
            +string DisplayName
            +string SourcePath
            +string TargetPath
            +JobState State
            +bool IsDifferential
            +bool IsActive
            +bool IsPaused
            +bool ShowControls
            +string PauseResumeText
            +string CancelText
            +ICommand PauseResumeCommand
            +ICommand CancelCommand
            +bool IsChecked
            +double ProgressPercent
            +string StateText
            +event PropertyChangedEventHandler? PropertyChanged
            +JobViewModel(int id, BackupJob job, Dispatcher dispatcher, Action onCheckedChanged)
            -TogglePauseResume() void
            +Detach() void
            +RefreshLocalizedTexts() void
            -OnPropertyChanged(string? propertyName) void
        }
        
        %% UI Components
        class MainWindow {
            +MainWindow()
            -InitializeComponent() void
            -Window_Loaded(object sender, RoutedEventArgs e) void
        }
        
        class App {
            +App()
            -InitializeComponent() void
            -Application_Startup(object sender, StartupEventArgs e) void
        }
    }
    
    %% =====================================================
    %% EASYSAVE.CONSOLE NAMESPACE (.NET 10)
    %% =====================================================
    
    namespace EasySave_Console {
        %% Console View Layer
        class ConsoleView {
            -MainViewModel _viewModel
            +ConsoleView(MainViewModel viewModel)
            +DisplayMenu() void
            -ExecuteSingleJobMenu() void
            -ExecuteAllJobsMenu() void
            -ExecuteCheckedJobsMenu() void
            -CreateJobMenu() void
            -ModifyJobMenu() void
            -DeleteJobMenu() void
            -ChangeSettingsMenu() void
            -ChangeLanguage() void
            -ChangeLogFormat() void
            -ChangeExtension() void
            -ChangeBusinessSoftware() void
            -ChangeEncryptionKey() void
            -DisplayJobList() void
            -GetString(string key) string
        }
        
        %% Console View Model
        class ConsoleMainViewModel {
            -BackupManager _backupManager
            -SettingsManager _settingsManager
            -AppSettings _currentSettings
            +ConsoleMainViewModel()
            +ExecuteJobCommand(int jobId) void
            +ExecuteAllCommand() void
            +ExecuteJobsFromCommandLine(string input) void
            +CreateJobCommand(string name, string source, string target, int type) void
            +ModifyJobCommand(int index, string name, string source, string target, int type) void
            +DeleteJobCommand(int index) void
            +GetJobs() List~BackupJob~
            +GetSettings() AppSettings
            +SaveSettings(AppSettings settings) void
            +GetString(string key) string
        }
        
        %% Program Entry
        class ConsoleProgram {
            +static Main(string[] args) void
            +static Main(string[] args) Task
        }
    }
    
    %% =====================================================
    %% EASYSAVE.TESTS NAMESPACE
    %% =====================================================
    
    namespace EasySave_Tests {
        class BackupJobTests {
            +TestPauseResume() void
            +TestCancel() void
            +TestProgressUpdates() void
        }
        
        class StrategyTests {
            +TestFullBackupCreatesAllFiles() void
            +TestDifferentialBackupSkipsUnmodified() void
            +TestEncryption() void
        }
        
        class StateManagerTests {
            +TestStateJsonAccumulation() void
            +TestStateHistory() void
        }
    }
    
    %% =====================================================
    %% RELATIONSHIPS AND DEPENDENCIES
    %% =====================================================
    
    %% EasyLog Relationships
    EasyLogger --> LogEntry : uses
    EasyLogger --> ILogWriter : uses
    JsonLogWriter ..|> ILogWriter : implements
    XMLLogWriter ..|> ILogWriter : implements
    
    %% EasySave.Core Relationships
    BackupJob --> IBackupStrategy : uses
    BackupJob --> JobState : uses
    BackupJob --> BusinessSoftwareService : uses
    BackupJob --> EncryptionService : uses
    BackupJob --> AppSettings : uses
    BackupJob --> EasyLogger : uses
    
    BackupManager --> BackupJob : manages
    BackupManager --> IConfigManager : uses
    BackupManager --> SettingsManager : uses
    BackupManager --> StateManager : uses
    BackupManager --> FullBackupStrategy : creates
    BackupManager --> DifferentialBackupStrategy : creates
    
    ConfigManager ..|> IConfigManager : implements
    SettingsManager --> AppSettings : manages
    SettingsManager --> SecurityHelper : uses
    
    StateManager --> BackupJob : observes
    StateManager --> JobStateData : manages
    
    EncryptionService --> AppSettings : uses
    
    FullBackupStrategy ..|> IBackupStrategy : implements
    DifferentialBackupStrategy ..|> IBackupStrategy : implements
    
    %% WPF Relationships
    MainViewModel --> BackupManager : uses
    MainViewModel --> SettingsManager : uses
    MainViewModel --> AppSettings : uses
    MainViewModel --> JobViewModel : manages
    MainViewModel --> RelayCommand : uses
    MainViewModel --> AsyncRelayCommand : uses
    
    JobViewModel --> BackupJob : wraps
    JobViewModel --> JobState : uses
    JobViewModel --> RelayCommand : uses
    
    MainWindow --> MainViewModel : uses
    App --> MainWindow : creates
    
    %% Console Relationships
    ConsoleView --> ConsoleMainViewModel : uses
    ConsoleMainViewModel --> BackupManager : uses
    ConsoleMainViewModel --> SettingsManager : uses
    ConsoleProgram --> ConsoleView : uses
    
    %% Test Relationships
    BackupJobTests --> BackupJob : tests
    StrategyTests --> FullBackupStrategy : tests
    StrategyTests --> DifferentialBackupStrategy : tests
    StateManagerTests --> StateManager : tests
```
