using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuhtaServer
{

    public class UserSession
    {
        public Guid SessionId;
        public string AuthToken;
        public Guid UserId;
        public string Login;
        public string Ip;
        public bool IsAdmin;
        public ConcurrentDictionary<string, WebSocket> webSockets = new ConcurrentDictionary<string, WebSocket>();
        public async void SendMessage(string clientWindowId, string message)
        {
            WebSocket webSocket;
            if (webSockets.TryGetValue(clientWindowId, out webSocket))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                try
                {
                    await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception e)
                {
                    Console.WriteLine("webSocket: ошибка отправки сообщения на "+clientWindowId+", "+e.Message );
                }
            }

        }

    }

    public static class Auth
    {

        public static ConcurrentDictionary<Guid, UserSession> UserSessions = new ConcurrentDictionary<Guid, UserSession>();

        //public static bool AdminAuthOk(sessionId)
        //{

        //}
    }
}
