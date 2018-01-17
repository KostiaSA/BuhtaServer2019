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
    [Route("api/checkAuth")]
    public class CheckAuthController : BaseAdminController
    {
        //class ResponseObject
        //{
        //    public string sql;
        //}

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {

            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                return new { ok = AuthOk((Guid)request["sessionId"], (String)request["authToken"]) };

            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
