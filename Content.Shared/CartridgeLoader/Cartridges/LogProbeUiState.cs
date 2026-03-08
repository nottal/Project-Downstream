// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class LogProbeUiState : BoundUserInterfaceState
{
    public List<PulledAccessLog> PulledLogs { get; }

    public LogProbeUiState(List<PulledAccessLog> pulledLogs)
    {
        PulledLogs = pulledLogs;
    }
}
