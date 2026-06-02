using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Utils;

public static class Constants
{
    public static class Telemetry
    {
        public const string TraceSource = "Syncnet.Trace";
        public const string MeterName = "SyncnetPlatform";
    }
    public static class Endpoints
    {
        public const string GameSessionWebSocket = "/ws/gamesession";
    }
}
