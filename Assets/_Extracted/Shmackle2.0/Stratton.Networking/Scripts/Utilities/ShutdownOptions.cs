namespace Stratton.Networking
{
    public struct ShutdownOptions
    {
        internal readonly ShutdownReason ShutdownReason;
        internal readonly bool DestroyRunner;
        internal readonly bool Force;

        public ShutdownOptions(ShutdownReason shutdownReason, bool destroyRunner, bool force)
        {
            ShutdownReason = shutdownReason;
            DestroyRunner = destroyRunner;
            Force = force;
        }
    }

    public enum ShutdownReason
    {
        /// <summary>
        /// Fusion was shutdown by request.
        /// </summary>
        Ok = 0,
        /// <summary>
        /// Shutdown was caused by some internal error.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Raised when the peer tries to join a room with a mismatching type between ClientServer Mode and Shared Mode.
        /// </summary>
        IncompatibleConfiguration = 2,
        /// <summary>
        /// Raised when the local peer started as a Server and tried to join a room that already has a Server peer.
        /// </summary>
        ServerInRoom = 3,
        /// <summary>
        /// Raised when the peer is disconnected or kicked by a plugin logic.
        /// </summary>
        DisconnectedByPluginLogic = 4,
        /// <summary>
        /// Raised when the game the peer is trying to join is closed.
        /// </summary>
        GameClosed = 5,
        /// <summary>
        /// Raised when the game the peer is trying to join does not exist.
        /// </summary>
        GameNotFound = 6,
        /// <summary>
        /// Raised when all CCU available for the Photon Application are in use.
        /// </summary>
        MaxCcuReached = 7,
        /// <summary>
        /// Raised when the peer is trying to connect to an unavailable or non-existent region.
        /// </summary>
        InvalidRegion = 8,
        /// <summary>
        /// Raised when a session with the same name was already created.
        /// </summary>
        GameIdAlreadyExists = 9,
        /// <summary>
        /// Raised when a peer is trying to join a room with already the max capacity of players.
        /// </summary>
        GameIsFull = 10,
        /// <summary>
        /// Raised when the authentication values are invalid.
        /// </summary>
        InvalidAuthentication = 11,
        /// <summary>
        /// Raised when the custom authentication has failed for some other reason.
        /// </summary>
        CustomAuthenticationFailed = 12,
        /// <summary>
        /// Raised when the authentication ticket has expired.
        /// </summary>
        AuthenticationTicketExpired = 13,
        /// <summary>
        /// Timeout on the connection with the Photon Cloud.
        /// </summary>
        PhotonCloudTimeout = 14,
        /// <summary>
        /// Raised when Fusion is already running and the StartGame is invoked again.
        /// </summary>
        AlreadyRunning = 15,
        /// <summary>
        /// Raised when any of the StartGame arguments does not meet the requirements.
        /// </summary>
        InvalidArguments = 16,
        /// <summary>
        /// Signal this runner is shutting down because of a Host Migration is about to happen.
        /// </summary>
        HostMigration = 17,
        /// <summary>
        /// Connection with a remote server failed by timeout.
        /// </summary>
        ConnectionTimeout = 18,
        /// <summary>
        /// Connection with a remote server failed because it was refused.
        /// </summary>
        ConnectionRefused = 19
    }
}

