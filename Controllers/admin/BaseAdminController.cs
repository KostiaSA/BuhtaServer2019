using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                return authToken == UserSession.AuthToken && HttpContext.Connection.RemoteIpAddress.ToString()== UserSession.Ip;
            }
            else
            {
                var sql = "SELECT * FROM buhta_auth_Session WHERE sessionId=" + Utils.GuidAsSql(sessionId, App.AuthDb.Dialect)+ 
                    " AND authToken="+ Utils.StringAsSql(authToken, App.AuthDb.Dialect)+
                    " AND clientIp=" + Utils.StringAsSql(HttpContext.Connection.RemoteIpAddress.ToString(), App.AuthDb.Dialect);
                                
                var dataset = Utils.GetLoadedDataSet(App.AuthDb.Name, sql);
                if (dataset.Tables[0].Rows.Count == 0)
                {
                    Thread.Sleep(1000);
                    return false;
                }
                var row = dataset.Tables[0].Rows[0];
                var userSession = new UserSession()
                {
                    SessionId = sessionId,
                    UserId = (Guid)row["userId"],
                    Login = (string)row["login"],
                    IsAdmin= (bool)row["isAdmin"],
                    AuthToken = authToken,
                    Ip = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                Auth.UserSessions.AddOrUpdate(userSession.SessionId, userSession, (key, oldValue) => userSession);

                return true;

            }

        }

        protected Object NoAuthResponse()
        {
            return new { error = "Требуется авторизация" };
        }

    }
}