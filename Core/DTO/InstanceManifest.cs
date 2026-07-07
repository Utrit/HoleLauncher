using System;
using System.Collections.Generic;
using System.Reflection;

namespace HoleLauncher.Core.DTO;

public class InstanceManifest
{
    public string InstanceId { get; set; }
    public string InstanceName { get; set; }
    public string InstanceMCVersion { get; set; }
    public string InstanceForgeVersion { get; set; }
    public string InstanceNeoForgeVersion { get; set; }
    public List<ModEntry> Mods { get; set; }
    public List<string> SelectedMods { get; set; }
    public int InstanceVersion { get; set; }

    public InstanceManifest(string instanceId, string instanceName, string instanceMcVersion, string instanceForgeVersion, string instanceNeoForgeVersion, List<ModEntry> mods, int instanceVersion, List<string> selectedMods)
    {
        InstanceId = instanceId;
        InstanceName = instanceName;
        InstanceMCVersion = instanceMcVersion;
        InstanceForgeVersion = instanceForgeVersion;
        InstanceNeoForgeVersion = instanceNeoForgeVersion;
        Mods = mods;
        InstanceVersion = instanceVersion;
        SelectedMods = selectedMods;
    }
}

[Serializable]
public struct ModEntry
{
    public string ModName { get; set; }
    public string ModSlug { get; set; }
    public List<string> ModDepend { get; set; }
    
    public int ModVersion { get; set; }
    public string ModLink { get; set; }
    public string ModSHA512 { get; set; }
    public string ModServerSide { get; set; }
    public string ModClientSide { get; set; }
    public ModType ModType { get; set; }
    public string ModPath { get; set; }

    public ModEntry(string modName, int modVersion, string modLink, string modSha512, string modServerSide, string modClientSide, ModType modType, string modPath, string modSlug, List<string> modDepend)
    {
        ModName = modName;
        ModVersion = modVersion;
        ModLink = modLink;
        ModSHA512 = modSha512;
        ModServerSide = modServerSide;
        ModClientSide = modClientSide;
        ModType = modType;
        ModPath = modPath;
        ModSlug = modSlug;
        ModDepend = modDepend;
    }
}

public enum ModType
{
    Strong,
    Soft,
    Optional
}