using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoleLauncher.Core.DTO;
using HoleLauncher.Core.Launcher;
using HoleLauncher.Core.Services;
using HoleLauncher.Models;
using ReactiveUI;
using Splat;

namespace HoleLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILauncherCore? _launcher;
    private readonly IMessageBus? _messageBus;
    private IDataProvider? _dataProvider;
    
    private string _username = string.Empty;
    private string _ramAmount = string.Empty;
    private string _backendAddress = string.Empty;
    private string _filename = string.Empty;
    private UserDTO? _selectedUser = new("", "", "", "");
    private float _progress = 0f;
    private ReadOnlyObservableCollection<string> _availableOptions;
    private ManifestInfo _manifest;
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
    
    public string RamAmount
    {
        get => _selectedUser.RamAmount;
        set {
            if (_selectedUser.RamAmount != value)
            {
                _selectedUser.RamAmount = value;
                _messageBus?.SendMessage(_selectedUser, "Updated User");
            }
            this.RaiseAndSetIfChanged(ref _ramAmount, value); 
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
    private ObservableCollection<OptionalModModel> _options = new();
    

    public ObservableCollection<string> Instances
    {
        get => _instances;
        set => this.RaiseAndSetIfChanged(ref _instances, value);
    }

    public ObservableCollection<OptionalModModel> Optional
    {
        get => _options;
        set => this.RaiseAndSetIfChanged(ref _options, value);
    }
    
    private string? _selectedInstance = "None";
    public string SelectedInstance {
        get => _selectedUser.SelectedInstance;
        set {
            if (_selectedUser.SelectedInstance != value && value is not null)
            {
                _selectedUser.SelectedInstance = value;
                _messageBus?.SendMessage(_selectedUser, "Updated User");
                Optional = new ObservableCollection<OptionalModModel>(_manifest.InstanceManifests.FirstOrDefault(x=>$"{x.InstanceId}/{x.InstanceName}" == value).Mods.Where(x=>x.ModType == ModType.Optional).Select(x=> new OptionalModModel(x, false, _messageBus)));
            }
            this.RaiseAndSetIfChanged(ref _selectedInstance, value); 
        }
    }
    
    public MainWindowViewModel()
    {
        _launcher = Locator.Current.GetService<ILauncherCore>();
        _messageBus = Locator.Current.GetService<IMessageBus>();
        _dataProvider = Locator.Current.GetService<IDataProvider>();
        _messageBus?.Listen<UserDTO>("Updated UserUI").Subscribe(OnUserUpdated);
        _messageBus?.Listen<ProgressInfo>("Updated FileUI").Subscribe(OnProgressUpdate);
        _messageBus?.Listen<ManifestInfo>("Updated InstanceUI").Subscribe(OnManifestUpdated);
        _messageBus?.Listen<OptionModUIUpdate>("OptionModUIUpdate").Subscribe(OnOptionModUpdated);
    }

    private void OnOptionModUpdated(OptionModUIUpdate obj)
    {
        Optional.First(x => x.Name == obj.ModName).SetLocked(obj.Status);
    }
    
    private void OnManifestUpdated(ManifestInfo manifestInfo)
    {
        _manifest = manifestInfo;
        Instances = new ObservableCollection<string>(manifestInfo.InstanceManifests.Select(x => { return $"{x.InstanceId}/{x.InstanceName}"; }));
        var currManifest = _manifest.InstanceManifests.FirstOrDefault(x =>
            $"{x.InstanceId}/{x.InstanceName}" == _selectedUser.SelectedInstance);
        var manifestPath = $"./data/{SelectedInstance.Split("/")[1]}/InstanceManifest.json";
        var localManifest = _dataProvider?.Load<InstanceManifest>(manifestPath);

        if (localManifest is not null)
        {
            Optional = new ObservableCollection<OptionalModModel>(currManifest.Mods.Where(x=>x.ModType == ModType.Optional).Select(x=> new OptionalModModel(x, localManifest.SelectedMods.Any(y=> y == x.ModSlug), _messageBus)));
            foreach (var modSlug in localManifest.SelectedMods)
            {
                var mod = localManifest.Mods.FirstOrDefault(x => x.ModSlug == modSlug);
                _messageBus?.SendMessage(new OptionModSelect(mod, true), "OptionModSelect");
            }
        }
        else
        {
            Optional = new ObservableCollection<OptionalModModel>(currManifest.Mods.Where(x=>x.ModType == ModType.Optional).Select(x=> new OptionalModModel(x, false, _messageBus)));
        }
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
        RamAmount = user.RamAmount;
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