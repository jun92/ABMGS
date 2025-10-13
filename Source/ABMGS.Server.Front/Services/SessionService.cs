
using System.Net.WebSockets;

namespace ABMGS.Server.Front.Services
{
    public class SessionService : BackgroundService
    {
        private IDictionary<Guid, WebSocket> _sessions = new Dictionary<Guid, WebSocket>();
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).Wait(stoppingToken);
            }

            return Task.CompletedTask;
        }

        internal void AddWebSocket(Guid userId, WebSocket socket)
        {
            _sessions.Add(userId, socket);
        }
    }
}
