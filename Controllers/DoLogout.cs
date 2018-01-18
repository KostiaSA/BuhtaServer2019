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
    [Route("api/doLogout")]
    public class DoLogoutController : BaseAdminController
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

                var login = (string)request["login"];
                var sessionId = (Guid)request["sessionId"];

                    Auth.UserSessions.TryRemove(sessionId, out _);

                    var sqlBatch = new List<string>();
                    sqlBatch.Add("DELETE FROM buhta_auth_Session WHERE sessionId=" + Utils.GuidAsSql(sessionId, database.Dialect));

                    Utils.ExecuteSql("auth", sqlBatch.ToArray());

                    Console.WriteLine("успешный logout: " + login);
                    return new {  };
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
