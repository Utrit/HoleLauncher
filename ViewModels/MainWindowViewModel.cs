using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoleLauncher.Core.DTO;
using HoleLauncher.Core.Launcher;
using ReactiveUI;
using Splat;

namespace HoleLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILauncherCore? _launcher;
    private readonly IMessageBus? _messageBus;
    
    private string _username = string.Empty;
    private string _backendAddress = string.Empty;
    private string _filename = string.Empty;
    private UserDTO? _selectedUser = new UserDTO("", "", "");
    private float _progress = 0f;
    private ReadOnlyObservableCollection<string> _availableOptions;
    public string UserName
    {
        get => _selectedUser.Username;
        set {
            if (_selectedUser.Username != value)
            {
                _selectedUser.Username = value;
                _messageBus?.SendMessage(_selectedUser, "Updated User");
            }
            this.RaiseAndSetIfChanged(ref _username, value); 
        }
    }
    
    public string BackendAddress
    {
        get => _selectedUser.BackendAddress;
        set {
            if (_selectedUser.BackendAddress != value)
            {
                _selectedUser.BackendAddress = value;
                _messageBus?.SendMessage(_selectedUser, "Updated User");
            }
            this.RaiseAndSetIfChanged(ref _backendAddress, value); 
        }
    }

    
    public string FileName
    {
        get => _filename;
        set {
            this.RaiseAndSetIfChanged(ref _filename, value); 
        }
    }
    
    public float Progress
    {
        get => _progress;
        set {
            this.RaiseAndSetIfChanged(ref _progress, value); 
        }
    }

    
    private ObservableCollection<string> _instances = new();

    public ObservableCollection<string> Instances
    {
        get => _instances;
        set => this.RaiseAndSetIfChanged(ref _instances, value);
    }
    
    private string? _selectedInstance = "None";
    public string SelectedInstance {
        get => _selectedUser.SelectedInstance;
        set {
            if (_selectedUser.SelectedInstance != value && value is not null)
            {
                _selectedUser.SelectedInstance = value;
                _messageBus?.SendMessage(_selectedUser, "Updated User");
            }
            this.RaiseAndSetIfChanged(ref _selectedInstance, value); 
        }
    }
    
    public MainWindowViewModel()
    {
        _launcher = Locator.Current.GetService<ILauncherCore>();
        _messageBus = Locator.Current.GetService<IMessageBus>();
        _messageBus?.Listen<UserDTO>("Updated UserUI").Subscribe(OnUserUpdated);
        _messageBus?.Listen<ProgressInfo>("Updated FileUI").Subscribe(OnProgressUpdate);
        _messageBus?.Listen<ManifestInfo>("Updated InstanceUI").Subscribe(OnManifestUpdated);
    }

    private void OnManifestUpdated(ManifestInfo manifestInfo)
    {
        Instances = new ObservableCollection<string>(manifestInfo.InstanceManifests.Select(x => { return $"{x.InstanceId}/{x.InstanceName}"; }));
    }
    
    private void OnProgressUpdate(ProgressInfo data)
    {
        FileName = data.FileName;
        Progress = data.PercentComplete;
        Console.WriteLine($"Progress: {data.PercentComplete}% of {data.FileName}");
    }
    
    private void OnUserUpdated(UserDTO? user)
    {
        if (user == null) return;
        _selectedUser = user;
        UserName = user.Username;
        BackendAddress = user.BackendAddress;
        SelectedInstance = user.SelectedInstance;
    }

    public void OnStartGame()
    {
        _launcher?.StartGame();
    }

    public void OnIntegrityCheck()
    {
        _launcher?.StartIntegrityCheck();
    }
}