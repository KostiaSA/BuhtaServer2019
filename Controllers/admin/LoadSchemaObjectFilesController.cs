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
    [Route("api/admin/loadSchemaObjectFiles")]
    public class AdminLoadSchemaObjectFilesController : BaseAdminController
    {
        class ResponseObject
        {
            public string json;
            public string jsx;
        }

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {
            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                    return NoAuthResponse();


                var jsonPath = App.GetWebRoot() + "/" + request["filePath"] + ".json";
                var res = new ResponseObject();
                var ok = false;
                if (System.IO.File.Exists(jsonPath))
                {
                    res.json = System.IO.File.ReadAllText(jsonPath, Encoding.UTF8);
                    ok = true;
                }

                var jsxPath = App.GetWebRoot() + "/" + request["filePath"] + ".jsx";
                if (System.IO.File.Exists(jsxPath))
                {
                    res.json = System.IO.File.ReadAllText(jsxPath, Encoding.UTF8);
                    ok = true;
                }


                if (!ok)
                    return new { error = "не найден файл '"+ request["filePath"] + ".json (или .jsx)'" };
                else
                    return res;
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
