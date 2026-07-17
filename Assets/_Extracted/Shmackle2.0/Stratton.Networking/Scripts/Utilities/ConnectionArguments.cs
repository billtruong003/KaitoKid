using Fusion.Photon.Realtime;
using System.Collections.Generic;

namespace Stratton.Networking
{
    public struct ConnectionArguments
    {
        public string GameMode;
        public string SessionName;
        public string Ip;
        public ushort? Port;
        public string CustomPublicIp;
        public ushort? CustomPublicIpPort;
        public string CustomLobbyName;
        public bool DisableClientSessionCreation;
        public Dictionary<string, object> SessionProperties;
        public byte[] ConnectionToken;
        public FusionAppSettings AppSettings;
    }
}