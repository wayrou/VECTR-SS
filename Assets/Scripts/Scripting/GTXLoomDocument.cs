using System;
using UnityEngine;

namespace GTX.Scripting
{
    [Serializable]
    public sealed class GTXLoomDocument
    {
        public string name = "gtx_script";
        [TextArea(8, 24)] public string source = "loom(gtx_script)\n\nbutton(reset_zone)\ndo(\n  reset hazard(all)\n)";
        public GTXLoomBindingStatus status = GTXLoomBindingStatus.Unbound;
        public GTXLoomCapability capabilities = GTXLoomCapability.ReadRaceState | GTXLoomCapability.TriggerCosmeticFeedback;

        public bool CanAffectGameplay => (capabilities & GTXLoomCapability.ModifyRaceRules) != 0
            || (capabilities & GTXLoomCapability.ControlVehicles) != 0
            || (capabilities & GTXLoomCapability.ControlLobby) != 0;
    }

    public enum GTXLoomBindingStatus
    {
        Unbound,
        Bound,
        Invalid
    }

    [Flags]
    public enum GTXLoomCapability
    {
        None = 0,
        ReadRaceState = 1 << 0,
        TriggerCosmeticFeedback = 1 << 1,
        ModifyRaceRules = 1 << 2,
        ControlVehicles = 1 << 3,
        ControlLobby = 1 << 4,
        SpawnObjects = 1 << 5
    }
}
