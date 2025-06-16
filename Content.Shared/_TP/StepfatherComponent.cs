using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._TP;

[RegisterComponent]
public sealed partial class StepfatherComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}
