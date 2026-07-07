namespace HoleLauncher.Core.DTO;

public class OptionModUIUpdate
{
    public string ModName;
    public bool Status;

    public OptionModUIUpdate(string modName, bool status)
    {
        ModName = modName;
        Status = status;
    }
}