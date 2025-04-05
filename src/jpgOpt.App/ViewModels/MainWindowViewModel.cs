using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using jpgOpt.App.Commands;
using jpgOpt.App.Models;
using jpgOpt.App.Services;
using pawKit.Core.IO;

namespace jpgOpt.App.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly SessionManager _sessionManager;
        private Session? _currentSession;

        private InputImage? _selectedImage;

        private bool _enableBlackPointAdjustment = SessionDefaults.EnableBlackPointAdjustment;
        private float _linearStretchBlackPointPercentage = SessionDefaults.LinearStretchBlackPointPercentage;
        private bool _enableWhitePointAdjustment = SessionDefaults.EnableWhitePointAdjustment;
        private float _linearStretchWhitePointPercentage = SessionDefaults.LinearStretchWhitePointPercentage;
        private float _saturationPercentage = SessionDefaults.SaturationPercentage;
        private bool _adaptiveSharpen = SessionDefaults.AdaptiveSharpen;

        private bool _removeGps = SessionDefaults.RemoveGps;
        private bool _removeAllMetadata = SessionDefaults.RemoveAllMetadata;
        private uint _jpegQuality = SessionDefaults.JpegQuality;
        private bool _isSessionLocked = SessionDefaults.IsSessionLocked;

        private ICommand? _newSessionCommand;
        private ICommand? _openSessionCommand;
        private ICommand? _addImagesCommand;
        private ICommand? _closeWindowCommand;
        private ICommand? _resetAllParametersCommand;
        private ICommand? _setSaturationCommand;
        private ICommand? _optimizeCommand;
        private ICommand? _deleteSelectedCommand;

        public MainWindowViewModel()
        {
            _sessionManager = new SessionManager();

            InitializeNewSession();
        }

        private void InitializeNewSession()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outputDirectoryPath;

            while (true)
            {
                string randomDirectoryName = $"jpgOpt-{DateTime.Now:yyyyMMdd'-'HHmmss}";
                outputDirectoryPath = PathOperations.CombineAndNormalizeAbsolutePath(desktopPath, randomDirectoryName);

                if (!Directory.Exists(outputDirectoryPath) && !File.Exists(outputDirectoryPath))
                    break;

                Thread.Sleep(1000);
            }

            _currentSession = _sessionManager.CreateSession(outputDirectoryPath);

            _enableBlackPointAdjustment = SessionDefaults.EnableBlackPointAdjustment;
            _linearStretchBlackPointPercentage = SessionDefaults.LinearStretchBlackPointPercentage;
            _enableWhitePointAdjustment = SessionDefaults.EnableWhitePointAdjustment;
            _linearStretchWhitePointPercentage = SessionDefaults.LinearStretchWhitePointPercentage;
            _saturationPercentage = SessionDefaults.SaturationPercentage;
            _adaptiveSharpen = SessionDefaults.AdaptiveSharpen;

            _removeGps = SessionDefaults.RemoveGps;
            _removeAllMetadata = SessionDefaults.RemoveAllMetadata;
            _jpegQuality = SessionDefaults.JpegQuality;
            _isSessionLocked = SessionDefaults.IsSessionLocked;

            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(PendingTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));

            OnPropertyChanged(nameof(SelectedImage));

            OnPropertyChanged(nameof(EnableBlackPointAdjustment));
            OnPropertyChanged(nameof(LinearStretchBlackPointPercentage));
            OnPropertyChanged(nameof(EnableWhitePointAdjustment));
            OnPropertyChanged(nameof(LinearStretchWhitePointPercentage));
            OnPropertyChanged(nameof(SaturationPercentage));
            OnPropertyChanged(nameof(AdaptiveSharpen));

            OnPropertyChanged(nameof(RemoveGps));
            OnPropertyChanged(nameof(RemoveAllMetadata));
            OnPropertyChanged(nameof(JpegQuality));
            OnPropertyChanged(nameof(IsSessionLocked));
        }

        public int ImageCount => _currentSession?.InputImages.Count ?? 0;

        public int PendingTasksCount => _currentSession?.OptimizationTasks.Count(t => t.CompletedAtUtc == null) ?? 0;

        public int CompletedTasksCount => _currentSession?.OptimizationTasks.Count(t => t.CompletedAtUtc != null) ?? 0;

        public InputImage? SelectedImage
        {
            get => _selectedImage;

            set
            {
                if (_selectedImage != value)
                {
                    _selectedImage = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                    {
                        EnableBlackPointAdjustment = _selectedImage.EnableBlackPointAdjustment;
                        LinearStretchBlackPointPercentage = _selectedImage.LinearStretchBlackPointPercentage;
                        EnableWhitePointAdjustment = _selectedImage.EnableWhitePointAdjustment;
                        LinearStretchWhitePointPercentage = _selectedImage.LinearStretchWhitePointPercentage;
                        SaturationPercentage = _selectedImage.SaturationPercentage;
                        AdaptiveSharpen = _selectedImage.AdaptiveSharpen;
                    }
                }
            }
        }

        public bool EnableBlackPointAdjustment
        {
            get => _enableBlackPointAdjustment;

            set
            {
                if (_enableBlackPointAdjustment != value)
                {
                    _enableBlackPointAdjustment = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.EnableBlackPointAdjustment = value;
                }
            }
        }

        public float LinearStretchBlackPointPercentage
        {
            get => _linearStretchBlackPointPercentage;

            set
            {
                if (_linearStretchBlackPointPercentage != value)
                {
                    _linearStretchBlackPointPercentage = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.LinearStretchBlackPointPercentage = value;
                }
            }
        }

        public bool EnableWhitePointAdjustment
        {
            get => _enableWhitePointAdjustment;

            set
            {
                if (_enableWhitePointAdjustment != value)
                {
                    _enableWhitePointAdjustment = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.EnableWhitePointAdjustment = value;
                }
            }
        }

        public float LinearStretchWhitePointPercentage
        {
            get => _linearStretchWhitePointPercentage;

            set
            {
                if (_linearStretchWhitePointPercentage != value)
                {
                    _linearStretchWhitePointPercentage = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.LinearStretchWhitePointPercentage = value;
                }
            }
        }

        public float SaturationPercentage
        {
            get => _saturationPercentage;

            set
            {
                if (_saturationPercentage != value)
                {
                    _saturationPercentage = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.SaturationPercentage = value;
                }
            }
        }

        public bool AdaptiveSharpen
        {
            get => _adaptiveSharpen;

            set
            {
                if (_adaptiveSharpen != value)
                {
                    _adaptiveSharpen = value;
                    OnPropertyChanged();

                    if (_selectedImage != null)
                        _selectedImage.AdaptiveSharpen = value;
                }
            }
        }

        public bool RemoveGps
        {
            get => _removeGps;

            set
            {
                if (_removeGps != value)
                {
                    _removeGps = value;
                    OnPropertyChanged();

                    if (_currentSession != null)
                        _currentSession.RemoveGps = value;
                }
            }
        }

        public bool RemoveAllMetadata
        {
            get => _removeAllMetadata;

            set
            {
                if (_removeAllMetadata != value)
                {
                    _removeAllMetadata = value;
                    OnPropertyChanged();

                    if (_currentSession != null)
                        _currentSession.RemoveAllMetadata = value;
                }
            }
        }

        public uint JpegQuality
        {
            get => _jpegQuality;

            set
            {
                if (_jpegQuality != value)
                {
                    _jpegQuality = value;
                    OnPropertyChanged();

                    if (_currentSession != null)
                        _currentSession.JpegQuality = value;
                }
            }
        }

        public bool IsSessionLocked
        {
            get => _isSessionLocked;

            set
            {
                if (_isSessionLocked != value)
                {
                    _isSessionLocked = value;
                    OnPropertyChanged();

                    if (_currentSession != null)
                        _currentSession.IsSessionLocked = value;
                }
            }
        }

        public ICommand NewSessionCommand => _newSessionCommand ??= new RelayCommand(
            param => ExecuteNewSession(),
            param => CanExecuteNewSession()
        );

        public ICommand OpenSessionCommand => _openSessionCommand ??= new RelayCommand(
            param => ExecuteOpenSession(),
            param => CanExecuteOpenSession()
        );

        public ICommand AddImagesCommand => _addImagesCommand ??= new RelayCommand(
            param => ExecuteAddImages(),
            param => CanExecuteAddImages()
        );

        public ICommand CloseWindowCommand => _closeWindowCommand ??= new RelayCommand(
            param => ExecuteCloseWindow(),
            param => CanExecuteCloseWindow()
        );

        public ICommand ResetAllParametersCommand => _resetAllParametersCommand ??= new RelayCommand(
            param => ExecuteResetAllParameters(),
            param => CanExecuteResetAllParameters()
        );

        public ICommand SetSaturationCommand => _setSaturationCommand ??= new RelayCommand(
            param => ExecuteSetSaturation(param),
            param => CanExecuteSetSaturation()
        );

        public ICommand OptimizeCommand => _optimizeCommand ??= new RelayCommand(
            param => ExecuteOptimize(),
            param => CanExecuteOptimize()
        );

        public ICommand DeleteSelectedCommand => _deleteSelectedCommand ??= new RelayCommand(
            param => ExecuteDeleteSelected(),
            param => CanExecuteDeleteSelected()
        );

        private bool CanExecuteNewSession()
        {
            return true;
        }

        private void ExecuteNewSession()
        {
        }

        private bool CanExecuteOpenSession()
        {
            return true;
        }

        private void ExecuteOpenSession()
        {
        }

        private bool CanExecuteAddImages()
        {
            return true;
        }

        private void ExecuteAddImages()
        {
        }

        private bool CanExecuteCloseWindow()
        {
            return true;
        }

        private void ExecuteCloseWindow()
        {
        }

        private bool CanExecuteResetAllParameters()
        {
            return true;
        }

        private void ExecuteResetAllParameters()
        {
        }

        private bool CanExecuteSetSaturation()
        {
            return true;
        }

        private void ExecuteSetSaturation(object? param)
        {
        }

        private bool CanExecuteOptimize()
        {
            return true;
        }

        private void ExecuteOptimize()
        {
        }

        private bool CanExecuteDeleteSelected()
        {
            return true;
        }

        private void ExecuteDeleteSelected()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}