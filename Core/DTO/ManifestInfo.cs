using System.Collections.Generic;

namespace HoleLauncher.Core.DTO;

public class ManifestInfo
{
    public List<InstanceManifest> InstanceManifests { get; set; }

    public ManifestInfo(List<InstanceManifest> instanceManifests)
    {
        InstanceManifests = instanceManifests;
    }
}