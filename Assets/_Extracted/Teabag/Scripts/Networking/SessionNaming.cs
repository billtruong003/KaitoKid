namespace Teabag.Networking
{
    /// <summary>
    /// Helpers for composing deterministic Photon session names from a short random
    /// code plus the build-specific prefix. Keeping this in one place ensures the
    /// SpaceStation → WaitingZone and WaitingZone → FreeForAll hops produce
    /// identical session names across all participants of the same build.
    /// </summary>
    public static class SessionNaming
    {
        /// <summary>
        /// Combines the build prefix (from FusionSettings) with a short random code.
        /// Returns just the code when no prefix is configured.
        /// </summary>
        public static string BuildFullSessionName(string code)
        {
            var prefix = NetworkManager.GetSessionNamePrefix();
            return string.IsNullOrEmpty(prefix) ? code : $"{prefix}_{code}";
        }
    }
}
