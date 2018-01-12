using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer.Controllers
{
    public class BaseAdminController : Controller
    {
        protected UserSession UserSession;

        protected bool AuthOk(Guid sessionId, string authToken)
        {
            Console.WriteLine("Auth: " + sessionId + ",  " + authToken);

            var sessionOk = Auth.UserSessions.TryGetValue(sessionId, out UserSession);

            if (sessionOk)
            {
                return authToken == UserSession.AuthToken;
            }

            return false;
        }

        protected Object NoAuthResponse()
        {
            return new { error = "Требуется авторизация" };
        }

    }
}