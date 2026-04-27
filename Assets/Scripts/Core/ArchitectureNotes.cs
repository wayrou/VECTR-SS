namespace GTX.Core
{
    /// <summary>
    /// Documentation anchor for the GTX vertical-slice architecture.
    /// This type intentionally has no runtime behavior.
    /// </summary>
    public static class ArchitectureNotes
    {
        public const string ResponsibilityMap =
            "Core coordinates shared contracts; Data stores tuning; Vehicle owns driving; " +
            "Combat owns contact moves and targets; Flow owns hidden style state; UI renders state; Visuals owns polish; " +
            "Customization, Multiplayer, Scripting, and Terminal define future extension contracts.";

        public const string MvpLoop =
            "Drive, shift, clutch, boost, drift, hit a target, raise hidden Flow, tune, and restart cleanly.";

        public const string PlatformDirection =
            "GTX should become a moddable low-art racing platform with lobby rule customization, Loom scripting, " +
            "and a QUAC terminal/CLI command surface, while keeping multiplayer-affecting packages validated.";
    }
}
