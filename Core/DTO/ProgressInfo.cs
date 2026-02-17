namespace HoleLauncher.Core.DTO;

public class ProgressInfo
{
    public string FileName;
    public float PercentComplete;

    public ProgressInfo(string fileName, float percentComplete)
    {
        FileName = fileName;
        PercentComplete = percentComplete;
    }
}