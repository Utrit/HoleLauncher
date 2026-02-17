using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using HoleLauncher.Core.DTO;
using HoleLauncher.Core.Mods;
using HoleLauncher.Core.Services;
using HoleLauncher.Core.Utils;
using ReactiveUI;
using Splat;

namespace HoleLauncher.Core.Launcher;

public class LauncherCore : ILauncherCore
{
    private readonly IMessageBus? _messageBus;
    private IDataProvider? _dataProvider;
    private UserDTO? _user;
    private bool _isRunning;
    private bool _isRunningMods;
    private List<InstanceManifest>? _loadedManifests = new();
    
    public LauncherCore()
    {
        _messageBus = Locator.Current.GetService<IMessageBus>();
        _messageBus?
            .Listen<UserDTO>("Updated User")
            .Subscribe(OnUserChanged);
        _messageBus?
            .Listen<OnAppInited>("Init")
            .Subscribe(OnInit);
    }

    private void OnInit(OnAppInited data)
    {
        _dataProvider = Locator.Current.GetService<IDataProvider>();
        UpdateUserUI(_dataProvider?.Load<UserDTO>());
        _messageBus?.SendMessage(_user, "Updated UserUI");
    }
    
    private void OnUserChanged(UserDTO user)
    {
        _dataProvider?.Save(user);
        UpdateUserUI(user);
    }

    private async Task UpdateUserUI(UserDTO user)
    {
        _user = user;
        if (_user.BackendAddress != string.Empty)
        {
            _loadedManifests = await Util.DownloadJsonAsync<List<InstanceManifest>>($"http://{_user.BackendAddress}/instances");
            if (_loadedManifests != null)
                _messageBus?.SendMessage(new ManifestInfo(_loadedManifests), "Updated InstanceUI");
        }
    }
    
    
    public async Task<InstanceManifest> StartIntegrityCheck()
    {
        if (_isRunningMods)
        {
            return null;
        }
        _isRunningMods = true;
        SendInfo("Start Mods checking", 0f);
        _loadedManifests = await Util.DownloadJsonAsync<List<InstanceManifest>>($"http://{_user.BackendAddress}/instances");
        var currManifest = _dataProvider?.Load<InstanceManifest>();
        var remoteManifest = _loadedManifests.FirstOrDefault(x => $"{x.InstanceId}/{x.InstanceName}".Equals(_user.SelectedInstance));
        var res = await SetupMods(currManifest, remoteManifest);
        SendInfo("Finish Mods checking", 100f);
        _dataProvider?.Save(res);
        _isRunningMods = false;
        return res;
    }
    
    public async Task StartGame()
    {
        if (_isRunning || _isRunningMods)
        {
            return;
        }

        var currManifest = await StartIntegrityCheck();
        var name = "";
        var procent = 0f;
        var instancePath = $"./data/{currManifest.InstanceName}";
        var path = new MinecraftPath(instancePath);
        var launcher = new MinecraftLauncher(path);
        var forgeInstaller = new ForgeInstaller(launcher);
        
        var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e => name = e.Name);
        var byteProgress = new Progress<ByteProgress>(e => SendInfo(name, (float)e.ToRatio() * 100f));
        
        var versionName = await forgeInstaller.Install(currManifest.InstanceMCVersion, currManifest.InstanceForgeVersion, new ForgeInstallOptions
        {
            FileProgress = fileProgress,
            ByteProgress = byteProgress,
            InstallerOutput = new Progress<string>(e => name = e),
        });

        await launcher.InstallAsync(versionName, fileProgress, byteProgress);
        SendInfo("Complete!", 0f);
        
        var launchOption = new MLaunchOption
        {
            MaximumRamMb = 4096,
            Session = MSession.CreateOfflineSession(_user.Username),
        };



        SendInfo("Launch", 0f);
        var process = await launcher.BuildProcessAsync(versionName, launchOption);
        var processWrapper = new ProcessWrapper(process);
        processWrapper.OutputReceived += (s, e) => SendInfo($"[GAME] {e}", 0f);
        processWrapper.StartWithEvents();
        var exitCode = await processWrapper.WaitForExitTaskAsync();
        SendInfo($"Exited with code {exitCode}", 0f);
        _isRunning = false;
    }

    private bool DiffManifest(InstanceManifest currManifest, InstanceManifest remoteManifest,out List<IModsHandle> handles)
    {
        handles = new List<IModsHandle>();
        if (currManifest is null) return true;
        bool hasChanges = currManifest.InstanceName != remoteManifest.InstanceName ||
                          currManifest.InstanceVersion != remoteManifest.InstanceVersion;
        
        foreach (var currManifestMod in currManifest.Mods)
        {
            var remoteMod = remoteManifest.Mods.FirstOrDefault(x=>x.ModName==currManifestMod.ModName);
            if (remoteMod.ModName is null)
            {
                hasChanges = true;
                handles.Add(new RemoveModsHandle(currManifestMod, $"./data/{currManifest.InstanceName}"));
                continue;
            }

            if (remoteMod.ModVersion == currManifestMod.ModVersion) continue;
            hasChanges = true;
            handles.Add(new RemoveModsHandle(remoteMod, $"./data/{currManifest.InstanceName}"));
        }
        
        return hasChanges;
    }
    
    private async Task<InstanceManifest> SetupMods(InstanceManifest? currManifest, InstanceManifest? remoteManifest)
    {
        var manifest = currManifest;
        if (remoteManifest != null && DiffManifest(currManifest, remoteManifest, out var diff))
        {
            foreach (var modsHandle in diff)
            {
                modsHandle.Execute();
            }
            manifest = remoteManifest;
        }
        var instancePath = $"./data/{manifest.InstanceName}";
        var tasks = manifest.Mods.Select(async manifestMod =>
        {
            if (await ValidateMod(manifestMod, instancePath)) return null;
            return InstallMod(manifestMod, instancePath, manifest);
        });
        await Task.WhenAll(tasks);
        return manifest;
    }
    
    private void SendInfo(string info, float progress)
    {
        _messageBus?.SendMessage(new ProgressInfo(info, progress), "Updated FileUI");
    }
    
    private async Task InstallMod(ModEntry mod, string installPath, InstanceManifest manifest)
    {
        using var client = new WebClient();
        Directory.CreateDirectory($"{installPath}{mod.ModPath}/");
        var fileName = $"{installPath}{mod.ModPath}/{mod.ModName}";
        await Util.DownloadFileAsync(HandleBackendLink(mod, manifest), fileName);
        SendInfo($"Install:{mod.ModName}", 0f);
    }

    private string HandleBackendLink(ModEntry entry, InstanceManifest manifest)
    {
        if (entry.ModLink.StartsWith("[BACKEND]:."))
        {
            var removed = entry.ModLink.Remove(0, "[BACKEND]:.".Length);
            return $"http://{_user.BackendAddress}/loadcontent?id={manifest.InstanceId}&filePath=.{removed}";
        }
        return entry.ModLink;
    }
    
    private async Task<bool> ValidateMod(ModEntry mod, string installPath)
    {
        var fileName = $"{installPath}{mod.ModPath}/{mod.ModName}";
        if (!File.Exists(fileName)) return false;
        if (mod.ModType == ModType.Soft || mod.ModType == ModType.Optional) return true;
        var t = await Util.GetFileSHA(fileName);
        return mod.ModSHA512 == await Util.GetFileSHA(fileName);
    }
}