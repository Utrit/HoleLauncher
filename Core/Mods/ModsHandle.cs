using System.IO;
using HoleLauncher.Core.DTO;

namespace HoleLauncher.Core.Mods;

public class RemoveModsHandle : IModsHandle
{
    private ModEntry _modEntry;
    private string _instancePath;

    public RemoveModsHandle(ModEntry modEntry, string instancePath)
    {
        _modEntry = modEntry;
        _instancePath = instancePath;
    }

    public void Execute()
    {
        var path = $"{_instancePath}{_modEntry.ModPath}/{_modEntry.ModName}";
        if(!File.Exists(path)) return;
        File.Delete(path);
    }
}

public class UpdateModsHandle : IModsHandle
{
    private ModEntry _modEntry;

    public UpdateModsHandle(ModEntry modEntry)
    {
        _modEntry = modEntry;
    }


    public void Execute()
    {
        throw new System.NotImplementedException();
    }
}

public interface IModsHandle
{
   public void Execute(); 
}