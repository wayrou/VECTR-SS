using System;
using System.Collections.Generic;
using GTX.Customization;

namespace GTX.Multiplayer
{
    [Serializable]
    public sealed class GTXLobbyConfig
    {
        public string lobbyName = "GTX Local Lobby";
        public string hostCallsign = "HOST";
        public string joinCode = "LOCAL";
        public int maxPlayers = 8;
        public GTXLobbyTrustPolicy trustPolicy = GTXLobbyTrustPolicy.PrivateCustom;
        public GTXLobbyRaceMode raceMode = GTXLobbyRaceMode.CombatRace;
        public bool combatEnabled = true;
        public bool flowDebugAllowed;
        public bool allowCustomCars = true;
        public bool allowCustomTracks = true;
        public bool allowLoomScripts;
        public List<GTXCustomizationManifest> requiredPackages = new List<GTXCustomizationManifest>();

        public bool CanUsePackage(GTXCustomizationManifest manifest)
        {
            if (manifest == null)
            {
                return false;
            }

            if (trustPolicy == GTXLobbyTrustPolicy.LocalSandbox)
            {
                return true;
            }

            if (trustPolicy == GTXLobbyTrustPolicy.PrivateCustom)
            {
                return manifest.trustLevel != GTXCustomizationTrustLevel.LocalOnly || allowCustomCars || allowCustomTracks || allowLoomScripts;
            }

            return manifest.AllowsMultiplayerUse;
        }
    }

    public enum GTXLobbyTrustPolicy
    {
        LocalSandbox,
        PrivateCustom,
        VerifiedPackagesOnly,
        TournamentSafe
    }

    public enum GTXLobbyRaceMode
    {
        TimeTrial,
        CombatRace,
        ArenaDuel,
        StuntFlow,
        ScriptedLoomEvent
    }
}
