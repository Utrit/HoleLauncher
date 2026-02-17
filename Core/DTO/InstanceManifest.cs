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
    public List<ModEntry> Mods { get; set; }
    public int InstanceVersion { get; set; }

    public InstanceManifest(string instanceId, string instanceName, string instanceMcVersion, string instanceForgeVersion, List<ModEntry> mods, int instanceVersion)
    {
        InstanceId = instanceId;
        InstanceName = instanceName;
        InstanceMCVersion = instanceMcVersion;
        InstanceForgeVersion = instanceForgeVersion;
        Mods = mods;
        InstanceVersion = instanceVersion;
    }
}

[Serializable]
public struct ModEntry
{
    public string ModName { get; set; }
    public int ModVersion { get; set; }
    public string ModLink { get; set; }
    public string ModSHA512 { get; set; }
    public string ModServerSide { get; set; }
    public string ModClientSide { get; set; }
    public ModType ModType { get; set; }
    public string ModPath { get; set; }

    public ModEntry(string modName, int modVersion, string modLink, string modSha512, string modServerSide, string modClientSide, ModType modType, string modPath)
    {
        ModName = modName;
        ModVersion = modVersion;
        ModLink = modLink;
        ModSHA512 = modSha512;
        ModServerSide = modServerSide;
        ModClientSide = modClientSide;
        ModType = modType;
        ModPath = modPath;
    }
}

public enum ModType
{
    Strong,
    Soft,
    Optional
}