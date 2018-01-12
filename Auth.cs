using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    }

    public static class Auth
    {

        public static ConcurrentDictionary<Guid, UserSession> UserSessions = new ConcurrentDictionary<Guid, UserSession>();

        //public static bool AdminAuthOk(sessionId)
        //{

        //}
    }
}
