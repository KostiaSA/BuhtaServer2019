using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json.Linq;

namespace BuhtaServer.Controllers
{


    [Produces("application/json")]
    [Route("api/doLogin")]
    public class DoLoginController : BaseAdminController
    {

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {

            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                var database = Program.BuhtaConfig.GetDatabase("auth");
                if (database == null)
                {
                    return new { error = "на бухта-сервере нет настроек для базы данных 'auth'" };
                }


                var sessionId = (Guid)request["sessionId"];
                var login = (String)request["login"];
                var password = (String)request["password"];

                if (login=="admin" && password=="admin")
                {

                    var newAuthToken = Utils.GetRandomString(32);

                    var userSession = new UserSession()
                    {
                        SessionId = (Guid)request["sessionId"],
                        Login = login,
                        AuthToken= newAuthToken,
                        Ip = HttpContext.Connection.RemoteIpAddress.ToString()
                    };

                    Auth.UserSessions.AddOrUpdate(userSession.SessionId, userSession, (key, oldValue) => userSession);

                    var sqlBatch = new List<string>();
                    sqlBatch.Add("BEGIN TRAN");

                    var sql = "DELETE FROM buhta_auth_Session WHERE sessionId="+Utils.GuidAsSql(sessionId, database.Dialect);
                    sqlBatch.Add(sql);

                    var sql2 = "INSERT INTO buhta_auth_Session(sessionId,clientIp,login,authToken,buhtaServerName,createTime,lastTime) VALUES(";
                    sql2 += " " + Utils.GuidAsSql(sessionId, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.Ip, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.Login, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.AuthToken, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(Program.BuhtaConfig.serverUniqueName, database.Dialect);
                    sql2 += "," + Utils.DateTimeAsSql(DateTime.Now, database.Dialect);
                    sql2 += "," + Utils.DateTimeAsSql(DateTime.Now, database.Dialect);
                    sql2 += ")";
                    sqlBatch.Add(sql2);

                    sqlBatch.Add("COMMIT");

                    Utils.ExecuteSql("auth",sqlBatch.ToArray());

                    return new { authToken = newAuthToken };
                }
                else
                {
                    return new { error = "неверный логин или пароль" };
                }
                //if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                //  return NoAuthResponse();
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
