using Stratton.Core;

namespace Stratton.Networking
{

    public enum MatchmakingResultCode
    {
        OK = 0,
        Error = 1,
    }

    public class MatchmakingResult : CommonResult
    {
        public readonly MatchmakingResultCode ResultCode;
        public readonly string IP;
        public readonly int Port;
        public ConnectionArguments ConnectionArguments;

        public MatchmakingResult(MatchmakingResultCode resultCode, string ip, int port, ConnectionArguments connectionArguments, string errorMessage = "")
        {
            IP = ip;
            Port = port;
            ResultCode = resultCode;
            ConnectionArguments = connectionArguments;
            ErrorMessage = errorMessage;
        }

        public new static MatchmakingResult Success(string ip, int port, ConnectionArguments connectionArguments)
        {
            return new MatchmakingResult(MatchmakingResultCode.OK, ip, port, connectionArguments);
        }


        public new static MatchmakingResult Error(string errorMessage)
        {
            Log.Error(NetworkingLogChannel.Matchmaking, errorMessage);
            return new MatchmakingResult(MatchmakingResultCode.Error, string.Empty, 0, new ConnectionArguments(), errorMessage);
        }
    }
}