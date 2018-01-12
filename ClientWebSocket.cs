using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace BuhtaServer
{
    public class ClientWebSocket
    {
        public static ConcurrentDictionary<string, WebSocket> ClientWebSockets = new ConcurrentDictionary<string, WebSocket>();

    }
}
