using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTX.Customization
{
    [Serializable]
    public sealed class GTXCustomizationManifest
    {
        public string schemaVersion = "0.1.0";
        public string packageId = "local.gtx.prototype";
        public string displayName = "Local GTX Prototype Package";
        public string author = "local";
        public GTXCustomizationTrustLevel trustLevel = GTXCustomizationTrustLevel.LocalOnly;
        public List<string> vehicleRecipes = new List<string>();
        public List<string> tuningProfiles = new List<string>();
        public List<string> trackRecipes = new List<string>();
        public List<string> lobbyRuleSets = new List<string>();
        public List<string> loomScripts = new List<string>();
        public List<string> quacCommands = new List<string>();

        public bool AllowsMultiplayerUse => trustLevel == GTXCustomizationTrustLevel.Verified || trustLevel == GTXCustomizationTrustLevel.Official;

        public string Describe()
        {
            return $"{displayName} ({packageId}) schema {schemaVersion}, trust {trustLevel}";
        }
    }

    public enum GTXCustomizationTrustLevel
    {
        LocalOnly,
        PrivateLobby,
        Verified,
        Official
    }

    public enum GTXCustomizationSurface
    {
        VehicleRecipe,
        TuningProfile,
        TrackRecipe,
        LobbyRuleSet,
        LoomScript,
        QuacCommand,
        CosmeticPng,
        LowPolyMeshRecipe
    }
}
