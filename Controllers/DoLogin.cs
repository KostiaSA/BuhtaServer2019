using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;

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

                var sql = "SELECT * FROM buhta_auth_User WHERE login=" + Utils.StringAsSql(login, App.AuthDb.Dialect);
                var dataset = Utils.GetLoadedDataSet(App.AuthDb.Name, sql);
                if (dataset.Tables[0].Rows.Count == 0)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("неверный логин, " + login);
                    return new { error = "неверный логин или пароль" };
                }

                var row = dataset.Tables[0].Rows[0];
                var isAdmin = (bool)row["isAdmin"];
                var passwordSha256base64 = (string)row["password"];


                if (passwordSha256base64 == Utils.CalcPasswordSha256Base64(login, password, isAdmin, Program.BuhtaConfig.appSecuritySeed))
                {

                    var newAuthToken = "at_" + Utils.GetRandomString(32 - 3);

                    var userSession = new UserSession()
                    {
                        SessionId = (Guid)request["sessionId"],
                        UserId = (Guid)row["userId"],
                        Login = login,
                        IsAdmin = (bool)row["isAdmin"],
                        AuthToken = newAuthToken,
                        Ip = HttpContext.Connection.RemoteIpAddress.ToString()
                    };

                    Auth.UserSessions.AddOrUpdate(userSession.SessionId, userSession, (key, oldValue) => userSession);

                    var sqlBatch = new List<string>();
                    sqlBatch.Add("BEGIN TRAN");

                    var sql1 = "DELETE FROM buhta_auth_Session WHERE sessionId=" + Utils.GuidAsSql(sessionId, database.Dialect);
                    sqlBatch.Add(sql1);

                    var sql2 = "INSERT INTO buhta_auth_Session(sessionId,clientIp,login,authToken,userId,isAdmin,buhtaServerName,createTime,lastTime) VALUES(";
                    sql2 += " " + Utils.GuidAsSql(sessionId, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.Ip, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.Login, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(userSession.AuthToken, database.Dialect);
                    sql2 += "," + Utils.GuidAsSql(userSession.UserId, database.Dialect);
                    sql2 += "," + Utils.BoolAsSql(userSession.IsAdmin, database.Dialect);
                    sql2 += "," + Utils.StringAsSql(Program.BuhtaConfig.serverUniqueName, database.Dialect);
                    sql2 += "," + Utils.DateTimeAsSql(DateTime.Now, database.Dialect);
                    sql2 += "," + Utils.DateTimeAsSql(DateTime.Now, database.Dialect);
                    sql2 += ")";
                    sqlBatch.Add(sql2);

                    sqlBatch.Add("COMMIT");

                    Utils.ExecuteSql("auth", sqlBatch.ToArray());

                    Console.WriteLine("успешный логин: " + login);
                    Thread.Sleep(200);
                    return new { authToken = userSession.AuthToken, userId = userSession.UserId };
                }
                else
                {
                    Console.WriteLine("неверный пароль для " + login);
                    Thread.Sleep(1000);
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
