using System.Net.Http;

namespace Stratton.Core
{
    public static class HttpClientResultMapper
    {
        public static DataLoadErrorCode GetContentLoadErrorCode(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.NotFound:
                    return DataLoadErrorCode.NotFound;
                default:
                    return DataLoadErrorCode.Unknown;
            }
        }
    }
}