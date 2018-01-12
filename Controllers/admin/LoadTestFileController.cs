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
    [Route("api/admin/loadTestFile")]
    public class AdminLoadTestFileController : BaseAdminController
    {
        class ResponseObject
        {
            public string code;
        }

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {
            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                    return NoAuthResponse();

                var fullPath = App.GetWebRoot() + "/" + request["filePath"];
                var res = new ResponseObject();
                if (System.IO.File.Exists(fullPath))
                {
                    res.code = System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
                    return res;

                }
                else
                    return new { error = "не найден файл '"+ request["filePath"] + ".json (или .jsx)'" };
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
