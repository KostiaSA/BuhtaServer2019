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
    [Route("api/admin/SaveSchemaObjectFiles")]
    public class AdminSaveSchemaObjectFilesController : BaseAdminController
    {

        [HttpPost]
        public object Post([FromBody] dynamic req)
        {
            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                    return NoAuthResponse();


                var folder = App.GetWebRoot() + "/" + request["filePath"];
                folder = folder.Remove(folder.LastIndexOf("/") + 1);
                System.IO.Directory.CreateDirectory(folder);

                JToken fakeOut;
                if (request.TryGetValue("json",out fakeOut))
                {
                    var jsonPath = App.GetWebRoot() + "/" + request["filePath"] + ".json";
                    System.IO.File.WriteAllText(jsonPath, request["json"].ToString(), Encoding.UTF8);
                }

                if (request.TryGetValue("jsx", out fakeOut))
                {
                    var jsxPath = App.GetWebRoot() + "/" + request["filePath"] + ".jsx";
                    System.IO.File.WriteAllText(jsxPath, request["jsx"].ToString(), Encoding.UTF8);
                }

                if (request.TryGetValue("sql", out fakeOut))
                {
                    var sqlPath = App.GetWebRoot() + "/" + request["filePath"] + ".sql";
                    System.IO.File.WriteAllText(sqlPath, request["sql"].ToString(), Encoding.UTF8);
                    SqlTemplate.CompiledTemplates.Clear();
                }

                //Thread.Sleep(100);

                return new { };
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
