using System.Threading.Tasks;
using HoleLauncher.Core.DTO;

namespace HoleLauncher.Core.Launcher;

public interface ILauncherCore
{
    public Task StartGame();
    public Task<InstanceManifest> StartIntegrityCheck();
}