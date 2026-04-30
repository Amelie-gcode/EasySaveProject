using EasySave.Models;
using EasySave.Strategies;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace EasySave.WPF
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BackupManager _backupManager;
        private readonly SettingsManager _settingsManager;
        private readonly AppSettings _currentSettings;
        private readonly Dispatcher _dispatcher;

        private bool _isBusy;
        private JobViewModel? _selectedJob;
        private string _jobName = string.Empty;
        private string _sourcePath = string.Empty;
        private string _targetPath = string.Empty;
        private bool _isDifferential;
        private string _selectedLanguage = "EN";
        private string _selectedLogFormat = "JSON";
        private string _encryptedExtensionsInput = string.Empty;
        private string _businessSoftwareInput = string.Empty;
        private string _encryptionKeyInput = string.Empty;
        private bool _isModifyPanelOpen;

        public ObservableCollection<JobViewModel> Jobs { get; } = new ObservableCollection<JobViewModel>();

        public JobViewModel? SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (Equals(_selectedJob, value)) return;
                _selectedJob = value;
                OnPropertyChanged();
                (ExecuteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (DeleteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ModifySelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

                if (_selectedJob != null)
                {
                    JobName = _selectedJob.DisplayName;
                    SourcePath = _selectedJob.SourcePath;
                    TargetPath = _selectedJob.TargetPath;
                    IsDifferential = _selectedJob.IsDifferential;
                }
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
                if (normalized != "EN" && normalized != "FR" && normalized != "ES" && normalized != "AR")
                    normalized = "EN";

                if (_selectedLanguage == normalized) return;
                _selectedLanguage = normalized;
                OnPropertyChanged();
            }
        }

        public string SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
                if (normalized != "JSON" && normalized != "XML")
                    normalized = "JSON";
                if (_selectedLogFormat == normalized) return;
                _selectedLogFormat = normalized;
                OnPropertyChanged();
            }
        }

        public string EncryptedExtensionsInput
        {
            get => _encryptedExtensionsInput;
            set
            {
                if (_encryptedExtensionsInput == value) return;
                _encryptedExtensionsInput = value;
                OnPropertyChanged();
            }
        }

        public string BusinessSoftwareInput
        {
            get => _businessSoftwareInput;
            set
            {
                if (_businessSoftwareInput == value) return;
                _businessSoftwareInput = value;
                OnPropertyChanged();
            }
        }

        public string EncryptionKeyInput
        {
            get => _encryptionKeyInput;
            set
            {
                if (_encryptionKeyInput == value) return;
                _encryptionKeyInput = value;
                OnPropertyChanged();
            }
        }

        public bool IsModifyPanelOpen
        {
            get => _isModifyPanelOpen;
            set
            {
                if (_isModifyPanelOpen == value) return;
                _isModifyPanelOpen = value;
                OnPropertyChanged();
            }
        }

        public string MenuTitleText => LocalizationManager.Instance.GetString("MenuTitle");
        public string LanguageLabelText => LocalizationManager.Instance.GetString("GuiLanguage");
        public string JobEditorTitleText => LocalizationManager.Instance.GetString("GuiJobEditor");
        public string BrowseText => LocalizationManager.Instance.GetString("GuiBrowse");
        public string DifferentialBackupText => LocalizationManager.Instance.GetString("GuiDifferentialBackup");
        public string CreateText => LocalizationManager.Instance.GetString("GuiCreate");
        public string ModifyText => LocalizationManager.Instance.GetString("GuiModify");
        public string DeleteText => LocalizationManager.Instance.GetString("GuiDelete");
        public string ExecuteCheckedText => LocalizationManager.Instance.GetString("GuiExecuteChecked");
        public string ExecuteAllText => LocalizationManager.Instance.GetString("GuiExecuteAll");
        public string CancelText => LocalizationManager.Instance.GetString("GuiCancel");
        public string SaveText => LocalizationManager.Instance.GetString("GuiSave");
        public string SettingsTitleText => LocalizationManager.Instance.GetString("GuiSettings");
        public string JobNameHeaderText => LocalizationManager.Instance.GetString("GuiJobName");
        public string SourceHeaderText => LocalizationManager.Instance.GetString("LabelSource");
        public string TargetHeaderText => LocalizationManager.Instance.GetString("LabelTarget");
        public string SettingsLanguageText => LocalizationManager.Instance.GetString("GuiSettingsLanguage");
        public string SettingsLogFormatText => LocalizationManager.Instance.GetString("GuiSettingsLogFormat");
        public string SettingsExtensionsText => LocalizationManager.Instance.GetString("GuiSettingsExtensions");
        public string SettingsBlockingAppsText => LocalizationManager.Instance.GetString("GuiSettingsBlockingApps");
        public string SettingsEncryptionKeyText => LocalizationManager.Instance.GetString("GuiSettingsEncryptionKey");

        public string LabelNameText => LocalizationManager.Instance.GetString("LabelName");
        public string LabelSourceText => LocalizationManager.Instance.GetString("LabelSource");
        public string LabelTargetText => LocalizationManager.Instance.GetString("LabelTarget");
        public string LabelStateText => LocalizationManager.Instance.GetString("LabelState");

        public string JobName
        {
            get => _jobName;
            set
            {
                if (_jobName == value) return;
                _jobName = value;
                OnPropertyChanged();
                RefreshCrudCanExecute();
            }
        }

        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath == value) return;
                _sourcePath = value;
                OnPropertyChanged();
                RefreshCrudCanExecute();
            }
        }

        public string TargetPath
        {
            get => _targetPath;
            set
            {
                if (_targetPath == value) return;
                _targetPath = value;
                OnPropertyChanged();
                RefreshCrudCanExecute();
            }
        }

        public bool IsDifferential
        {
            get => _isDifferential;
            set
            {
                if (_isDifferential == value) return;
                _isDifferential = value;
                OnPropertyChanged();
                RefreshCrudCanExecute();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                (ExecuteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ExecuteAllJobsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CreateJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (DeleteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ModifySelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (BrowseSourceCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (BrowseTargetCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand ExecuteAllJobsCommand { get; }
        public ICommand ExecuteCheckedJobsCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand DeleteSelectedJobCommand { get; }
        public ICommand ModifySelectedJobCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand ModifyJobCommand { get; }
        public ICommand OpenModifyPanelCommand { get; }
        public ICommand CloseModifyPanelCommand { get; }

        public MainViewModel()
        {
            _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _settingsManager = new SettingsManager();
            _currentSettings = _settingsManager.LoadSettings();
            _settingsManager.SaveSettings(_currentSettings);

            // Apply language to all string lookups.
            LocalizationManager.Instance.SetLanguage(_currentSettings.Language);
            _selectedLanguage = LocalizationManager.Instance.CurrentLanguage;
            _selectedLogFormat = string.IsNullOrWhiteSpace(_currentSettings.LogFormat) ? "JSON" : _currentSettings.LogFormat.ToUpperInvariant();
            _encryptedExtensionsInput = string.Join(", ", _currentSettings.EncryptedExtensions ?? new System.Collections.Generic.List<string>());
            _businessSoftwareInput = string.Join(", ", _currentSettings.BusinessSoftwareName ?? new System.Collections.Generic.List<string>());
            _encryptionKeyInput = _currentSettings.EncryptionKey ?? string.Empty;

            _backupManager = new BackupManager();

            LoadJobs();

            ExecuteSelectedJobCommand = new AsyncRelayCommand(
                execute: ExecuteSelectedJobAsync,
                canExecute: () => SelectedJob != null && !IsBusy);

            ExecuteAllJobsCommand = new AsyncRelayCommand(
                execute: ExecuteAllJobsAsync,
                canExecute: () => Jobs.Count > 0 && !IsBusy);

            ExecuteCheckedJobsCommand = new AsyncRelayCommand(
                execute: ExecuteCheckedJobsAsync,
                canExecute: () => Jobs.Count > 0 && Jobs.Any(j => j.IsChecked) && !IsBusy);

            BrowseSourceCommand = new RelayCommand(
                execute: () => BrowseAndSetPath(isSource: true),
                canExecute: () => !IsBusy);

            BrowseTargetCommand = new RelayCommand(
                execute: () => BrowseAndSetPath(isSource: false),
                canExecute: () => !IsBusy);

            CreateJobCommand = new AsyncRelayCommand(
                execute: CreateJobAsync,
                canExecute: CanCreateJob);

            DeleteSelectedJobCommand = new AsyncRelayCommand(
                execute: DeleteSelectedJobAsync,
                canExecute: () => SelectedJob != null && !IsBusy);

            ModifySelectedJobCommand = new AsyncRelayCommand(
                execute: ModifySelectedJobAsync,
                canExecute: CanModifySelectedJob);

            SaveSettingsCommand = new RelayCommand(
                execute: SaveSettings,
                canExecute: () => !IsBusy);

            DeleteJobCommand = new AsyncRelayCommand(
                execute: DeleteJobAsync,
                canExecute: parameter => parameter is JobViewModel && !IsBusy);

            ModifyJobCommand = new AsyncRelayCommand(
                execute: ModifyJobAsync,
                canExecute: parameter => parameter is JobViewModel && !IsBusy && HasValidJobInputs());

            OpenModifyPanelCommand = new RelayCommand(
                execute: OpenModifyPanel,
                canExecute: parameter => parameter is JobViewModel && !IsBusy);

            CloseModifyPanelCommand = new RelayCommand(
                execute: () => IsModifyPanelOpen = false,
                canExecute: () => !IsBusy);
        }

        private void LoadJobs()
        {
            foreach (var existing in Jobs)
            {
                existing.Detach();
            }
            Jobs.Clear();

            var jobs = _backupManager.GetJobs();
            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Jobs.Add(new JobViewModel(
                    id: i + 1,
                    job: job,
                    dispatcher: _dispatcher,
                    onCheckedChanged: RefreshExecutionCanExecute));
            }

            RefreshExecutionCanExecute();
        }

        private void RefreshExecutionCanExecute()
        {
            (ExecuteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ExecuteAllJobsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ExecuteCheckedJobsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DeleteSelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ModifySelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CreateJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DeleteJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ModifyJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (SaveSettingsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenModifyPanelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CloseModifyPanelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private bool HasValidJobInputs()
            => !string.IsNullOrWhiteSpace(JobName)
               && !string.IsNullOrWhiteSpace(SourcePath)
               && !string.IsNullOrWhiteSpace(TargetPath);

        private bool CanCreateJob()
            => !IsBusy && HasValidJobInputs() && Jobs.Count < 5;

        private bool CanModifySelectedJob()
            => !IsBusy && SelectedJob != null && HasValidJobInputs();

        private void RefreshCrudCanExecute()
        {
            (CreateJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ModifySelectedJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ModifyJobCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ApplyLanguage(string langCode)
        {
            LocalizationManager.Instance.SetLanguage(langCode);
            _currentSettings.Language = langCode;
            _settingsManager.SaveSettings(_currentSettings);

            OnPropertyChanged(nameof(MenuTitleText));
            OnPropertyChanged(nameof(LanguageLabelText));
            OnPropertyChanged(nameof(JobEditorTitleText));
            OnPropertyChanged(nameof(BrowseText));
            OnPropertyChanged(nameof(DifferentialBackupText));
            OnPropertyChanged(nameof(CreateText));
            OnPropertyChanged(nameof(ModifyText));
            OnPropertyChanged(nameof(DeleteText));
            OnPropertyChanged(nameof(ExecuteCheckedText));
            OnPropertyChanged(nameof(ExecuteAllText));
            OnPropertyChanged(nameof(CancelText));
            OnPropertyChanged(nameof(SaveText));
            OnPropertyChanged(nameof(SettingsTitleText));
            OnPropertyChanged(nameof(JobNameHeaderText));
            OnPropertyChanged(nameof(SourceHeaderText));
            OnPropertyChanged(nameof(TargetHeaderText));
            OnPropertyChanged(nameof(SettingsLanguageText));
            OnPropertyChanged(nameof(SettingsLogFormatText));
            OnPropertyChanged(nameof(SettingsExtensionsText));
            OnPropertyChanged(nameof(SettingsBlockingAppsText));
            OnPropertyChanged(nameof(SettingsEncryptionKeyText));
            OnPropertyChanged(nameof(LabelNameText));
            OnPropertyChanged(nameof(LabelSourceText));
            OnPropertyChanged(nameof(LabelTargetText));
            OnPropertyChanged(nameof(LabelStateText));

            foreach (var job in Jobs)
            {
                job.RefreshLocalizedTexts();
            }
        }

        private void BrowseAndSetPath(bool isSource)
        {
            string? selected = BrowseFolder(initialPath: isSource ? SourcePath : TargetPath);
            if (string.IsNullOrWhiteSpace(selected))
                return;

            if (isSource)
                SourcePath = selected;
            else
                TargetPath = selected;
        }

        private static string? BrowseFolder(string? initialPath)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = LocalizationManager.Instance.GetString("GuiSelectFolder"),
                UseDescriptionForTitle = true,
                SelectedPath = string.IsNullOrWhiteSpace(initialPath) ? string.Empty : initialPath,
                ShowNewFolderButton = true
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath
                : null;
        }

        private async Task CreateJobAsync()
        {
            if (!HasValidJobInputs()) return;

            try
            {
                IsBusy = true;
                await Task.Run(() => _backupManager.CreateJob(JobName.Trim(), SourcePath.Trim(), TargetPath.Trim(), IsDifferential));
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(LoadJobs);
            }
        }

        private async Task DeleteSelectedJobAsync()
        {
            if (SelectedJob == null) return;

            int index = SelectedJob.Id - 1;
            try
            {
                IsBusy = true;
                await Task.Run(() => _backupManager.DeleteJob(index));
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(() =>
                {
                    SelectedJob = null;
                    LoadJobs();
                });
            }
        }

        private async Task DeleteJobAsync(object? parameter)
        {
            if (parameter is not JobViewModel job) return;
            SelectedJob = job;
            await DeleteSelectedJobAsync();
        }

        private async Task ModifySelectedJobAsync()
        {
            if (SelectedJob == null) return;
            if (!HasValidJobInputs()) return;

            int index = SelectedJob.Id - 1;
            try
            {
                IsBusy = true;
                await Task.Run(() => _backupManager.ModifyJob(index, JobName.Trim(), SourcePath.Trim(), TargetPath.Trim(), IsDifferential));
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(() =>
                {
                    LoadJobs();
                    IsModifyPanelOpen = false;
                });
            }
        }

        private async Task ModifyJobAsync(object? parameter)
        {
            if (parameter is not JobViewModel job) return;
            SelectedJob = job;
            await ModifySelectedJobAsync();
        }

        private void OpenModifyPanel(object? parameter)
        {
            if (parameter is not JobViewModel job) return;
            SelectedJob = job;
            IsModifyPanelOpen = true;
        }

        private void SaveSettings()
        {
            _currentSettings.Language = SelectedLanguage;
            _currentSettings.LogFormat = SelectedLogFormat;
            _currentSettings.EncryptionKey = string.IsNullOrWhiteSpace(EncryptionKeyInput) ? "default" : EncryptionKeyInput.Trim();
            _currentSettings.EncryptedExtensions = ParseCsvList(EncryptedExtensionsInput, ensureDotPrefix: true);
            _currentSettings.BusinessSoftwareName = ParseCsvList(BusinessSoftwareInput, ensureDotPrefix: false);
            _settingsManager.SaveSettings(_currentSettings);
            ApplyLanguage(SelectedLanguage);
        }

        private static System.Collections.Generic.List<string> ParseCsvList(string? input, bool ensureDotPrefix)
        {
            return (input ?? string.Empty)
                .Split(',')
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => ensureDotPrefix && !part.StartsWith(".") ? "." + part : part)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task ExecuteSelectedJobAsync()
        {
            if (SelectedJob == null) return;

            try
            {
                IsBusy = true;
                await Task.Run(() => _backupManager.ExecuteJob(SelectedJob.Id));
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(LoadJobs);
            }
        }

        private async Task ExecuteAllJobsAsync()
        {
            try
            {
                IsBusy = true;
                await Task.Run(() => _backupManager.ExecuteAll());
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(LoadJobs);
            }
        }

        private async Task ExecuteCheckedJobsAsync()
        {
            var checkedJobs = Jobs.Where(j => j.IsChecked).Select(j => j.Id).ToArray();
            if (checkedJobs.Length == 0) return;

            try
            {
                IsBusy = true;
                await Task.Run(() =>
                {
                    foreach (var id in checkedJobs)
                    {
                        _backupManager.ExecuteJob(id);
                    }
                });
            }
            finally
            {
                IsBusy = false;
                _dispatcher.Invoke(LoadJobs);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public sealed class JobViewModel : INotifyPropertyChanged
        {
            private readonly BackupJob _job;
            private readonly Dispatcher _dispatcher;
            private readonly EventHandler _progressHandler;
            private readonly Action _onCheckedChanged;
            private bool _isChecked;

            public int Id { get; }

            public string DisplayName => _job.Name;

            public string SourcePath => _job.SourcePath;

            public string TargetPath => _job.TargetPath;

            public JobState State => _job.State;

            public bool IsDifferential => _job.GetStrategy() is DifferentialBackupStrategy;

            public bool IsActive => State == JobState.Active;
            public bool IsPaused => State == JobState.Paused;
            public bool ShowControls => State == JobState.Active || State == JobState.Paused;

            public string PauseResumeText
                => State == JobState.Paused
                    ? LocalizationManager.Instance.GetString("GuiResume")
                    : LocalizationManager.Instance.GetString("GuiPause");

            public string CancelText => LocalizationManager.Instance.GetString("GuiCancel");

            public ICommand PauseResumeCommand { get; }
            public ICommand CancelCommand { get; }

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked == value) return;
                    _isChecked = value;
                    OnPropertyChanged();
                    _onCheckedChanged();
                }
            }

            public double ProgressPercent
            {
                get
                {
                    if (State == JobState.Completed) return 100;
                    if (State == JobState.Inactive) return 0;
                    if (State == JobState.Cancelled) return 0;

                    var totalSize = _job.TotalSize;
                    if (totalSize <= 0) return 0;
                    var done = totalSize - _job.SizeRemaining;
                    if (done < 0) done = 0;
                    if (done > totalSize) done = totalSize;
                    return (double)done / totalSize * 100.0;
                }
            }

            public string StateText
            {
                get
                {
                    return State switch
                    {
                        JobState.Inactive => LocalizationManager.Instance.GetString("StateInactive"),
                        JobState.Active => LocalizationManager.Instance.GetString("StateActive"),
                        JobState.Paused => LocalizationManager.Instance.GetString("StatePaused"),
                        JobState.Completed => LocalizationManager.Instance.GetString("StateCompleted"),
                        JobState.Cancelled => LocalizationManager.Instance.GetString("StateCancelled"),
                        JobState.Error => LocalizationManager.Instance.GetString("StateError"),
                        _ => State.ToString()
                    };
                }
            }

            public JobViewModel(int id, BackupJob job, Dispatcher dispatcher, Action onCheckedChanged)
            {
                Id = id;
                _job = job;
                _dispatcher = dispatcher;
                _onCheckedChanged = onCheckedChanged;

                PauseResumeCommand = new RelayCommand(
                    execute: TogglePauseResume,
                    canExecute: () => ShowControls);

                CancelCommand = new RelayCommand(
                    execute: () => _job.RequestCancel(),
                    canExecute: () => ShowControls);

                // We keep the handler so the UI updates while the backup is running.
                _progressHandler = (_, __) => _dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(State));
                    OnPropertyChanged(nameof(StateText));
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(IsPaused));
                    OnPropertyChanged(nameof(ShowControls));
                    OnPropertyChanged(nameof(PauseResumeText));
                    OnPropertyChanged(nameof(CancelText));
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(SourcePath));
                    OnPropertyChanged(nameof(TargetPath));
                    OnPropertyChanged(nameof(IsDifferential));
                    OnPropertyChanged(nameof(ProgressPercent));

                    (PauseResumeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }));
                _job.ProgressUpdated += _progressHandler;
            }

            private void TogglePauseResume()
            {
                if (State == JobState.Active)
                    _job.RequestPause();
                else if (State == JobState.Paused)
                    _job.RequestResume();
            }

            public void Detach()
            {
                _job.ProgressUpdated -= _progressHandler;
            }

            public void RefreshLocalizedTexts()
            {
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(PauseResumeText));
                OnPropertyChanged(nameof(CancelText));
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
