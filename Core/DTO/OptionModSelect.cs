namespace HoleLauncher.Core.DTO;

public class OptionModSelect
{
    public ModEntry ModEntry;
    public bool Status;
    
    public OptionModSelect(ModEntry modEntry, bool status)
    {
        ModEntry = modEntry;
        Status = status;
    }
}