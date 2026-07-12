using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
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
    private int _currentLoading;
    private List<InstanceManifest>? _loadedManifests = new();
    private HashSet<ModEntry> _activeOptionalMods = new();
    
    public LauncherCore()
    {
        _messageBus = Locator.Current.GetService<IMessageBus>();
        _messageBus?
            .Listen<UserDTO>("Updated User")
            .Subscribe(OnUserChanged);
        _messageBus?
            .Listen<OnAppInited>("Init")
            .Subscribe(OnInit);
        _messageBus?
            .Listen<OptionModSelect>("OptionModSelect")
            .Subscribe(OnOptionalModSelect);
    }

    private void OnOptionalModSelect(OptionModSelect obj)
    {
        if (obj.ModEntry.ModSlug is null) return;
        if (!obj.Status)
        {
            TryDisableMod(obj.ModEntry);
            return;
        }

        if (_activeOptionalMods.Any(x=>x.ModSlug == obj.ModEntry.ModSlug))
        {
            return;
        }
        
        _activeOptionalMods.Add(obj.ModEntry);
        foreach (var dep in obj.ModEntry.ModDepend)
        {
            _messageBus?.SendMessage(new OptionModUIUpdate(dep, obj.Status), "OptionModUIUpdate");
        }
    }

    private void TryDisableMod(ModEntry modEntry)
    {
        if (!_activeOptionalMods.Any(x=>x.ModSlug == modEntry.ModSlug)) return;
        var hasActiveParent = _activeOptionalMods.FirstOrDefault(x => x.ModDepend.Any(y => y == modEntry.ModSlug));
        if (hasActiveParent.ModName is not null)
        {
            return;
        }

        _activeOptionalMods.RemoveWhere(x=> x.ModSlug == modEntry.ModSlug);
        _messageBus?.SendMessage(new OptionModUIUpdate(modEntry.ModSlug, false), "OptionModUIUpdate");
        foreach (var dep in modEntry.ModDepend)
        {
            var mod = _activeOptionalMods.FirstOrDefault(x => x.ModSlug == dep);
            if (mod.ModName is null) continue;
            TryDisableMod(mod);
        }
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
        _loadedManifests = await Util.DownloadJsonAsync<List<InstanceManifest>>($"http://{_user?.BackendAddress}/instances");
        if (_loadedManifests != null)
            _messageBus?.SendMessage(new ManifestInfo(_loadedManifests), "Updated InstanceUI");
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
        var manifestPath = $"./data/{_user.SelectedInstance.Split("/")[1]}/InstanceManifest.json";
        var currManifest = _dataProvider?.Load<InstanceManifest>(manifestPath);
        var remoteManifest = _loadedManifests.FirstOrDefault(x => $"{x.InstanceId}/{x.InstanceName}".Equals(_user.SelectedInstance));
        var res = await SetupMods(currManifest, remoteManifest);
        SendInfo("Finish Mods checking", 100f);
        _dataProvider?.Save(res, manifestPath);
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
        var neoForgeInstaller = new NeoForgeInstaller(launcher);

        var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e => name = e.Name);
        var byteProgress = new Progress<ByteProgress>(e => SendInfo(name, (float)e.ToRatio() * 100f));
        Task<string> version;
        string versionName = "";

        if (currManifest.InstanceForgeVersion is not null)
        {
            SendInfo($"Installing Forge:{currManifest.InstanceForgeVersion}",0f);
            version = forgeInstaller.Install(currManifest.InstanceMCVersion, currManifest.InstanceForgeVersion, new ForgeInstallOptions
            {
                FileProgress = fileProgress,
                ByteProgress = byteProgress,
                InstallerOutput = new Progress<string>(e => name = e),
            });
        }
        else
        {
            SendInfo($"Installing NeoForge:{currManifest.InstanceNeoForgeVersion}",0f);
            version = neoForgeInstaller.Install(currManifest.InstanceMCVersion, currManifest.InstanceNeoForgeVersion, new NeoForgeInstallOptions
            {
                FileProgress = fileProgress,
                ByteProgress = byteProgress,
                InstallerOutput = new Progress<string>(e => name = e),
            });
        }

        try
        {
            versionName = await version;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            SendInfo(e.Message, 0f);
            throw;
        }
        
        SendInfo($"Installing MC: {currManifest.InstanceMCVersion}", 100f);
        try
        {
            await launcher.InstallAsync(versionName, fileProgress, byteProgress);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            SendInfo(e.Message, 0f);
            throw;
        }
        SendInfo("Complete!", 100f);
        
        var launchOption = new MLaunchOption
        {
            MaximumRamMb = Math.Clamp(int.Parse(_user.RamAmount), 1024, int.MaxValue),
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

            var manifestModActive = currManifest.SelectedMods.Any(x => x == currManifestMod.ModSlug );
            var optionalModActive = _activeOptionalMods.Any(x=>x.ModSlug == currManifestMod.ModSlug);
            if (manifestModActive && !optionalModActive)
            {
                hasChanges = true;
                handles.Add(new RemoveModsHandle(currManifestMod, $"./data/{currManifest.InstanceName}"));
                continue;
            }
            if ((remoteMod.ModVersion == currManifestMod.ModVersion && !optionalModActive) || currManifestMod.ModType != ModType.Optional) continue;
            hasChanges = true;
            handles.Add(new RemoveModsHandle(remoteMod, $"./data/{currManifest.InstanceName}"));
        }

        if (currManifest.SelectedMods.Any(x => !_activeOptionalMods.Any(y => y.ModSlug == x)))
        {
            hasChanges = true;
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

        manifest.SelectedMods = _activeOptionalMods.Select(x => x.ModSlug).ToList();
        var instancePath = $"./data/{manifest.InstanceName}";
        var tasks = manifest.Mods.Select(async manifestMod =>
        {
            if (await ValidateMod(manifestMod, instancePath)) return false;
            return await InstallMod(manifestMod, instancePath, manifest);
        });
        var res = await Task.WhenAll(tasks);
        return manifest;
    }
    
    private void SendInfo(string info, float progress)
    {
        _messageBus?.SendMessage(new ProgressInfo(info, progress), "Updated FileUI");
    }
    
    private async Task<bool> InstallMod(ModEntry mod, string installPath, InstanceManifest manifest)
    {
        
        while (_currentLoading > 5)
        {
            await Task.Delay(100);
        }
        _currentLoading++;
        Directory.CreateDirectory($"{installPath}{mod.ModPath}/");
        var fileName = $"{installPath}{mod.ModPath}/{mod.ModName}";
        SendInfo($"Install:{mod.ModName}", 0f);
        await Util.DownloadFileAsync(HandleBackendLink(mod, manifest), fileName);
        SendInfo($"Installed:{mod.ModName}", 100f);
        _currentLoading--;
        return true;
    }

    private string HandleBackendLink(ModEntry entry, InstanceManifest manifest)
    {
        if (!entry.ModLink.StartsWith("[BACKEND]:.")) return entry.ModLink;
        var removed = entry.ModLink.Remove(0, "[BACKEND]:.".Length);
        return $"http://{_user?.BackendAddress}/loadcontent?id={manifest.InstanceId}&filePath=.{removed}";
    }
    
    private async Task<bool> ValidateMod(ModEntry mod, string installPath)
    {
        if (mod.ModClientSide == "unsupported") return true;
        var fileName = $"{installPath}{mod.ModPath}/{mod.ModName}";
        if (mod.ModType == ModType.Optional && _activeOptionalMods.All(x => x.ModSlug != mod.ModSlug)) return true;
        if (!File.Exists(fileName)) return false;
        if (mod.ModType == ModType.Soft || mod.ModType == ModType.Optional) return true;
        return mod.ModSHA512 == await Util.GetFileSHA(fileName);
    }
}