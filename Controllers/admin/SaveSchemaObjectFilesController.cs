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
        public object Post([FromBody] JObject req)
        {
            try
            {
                if (!AuthOk())
                    return NoAuthResponse();


                var folder = App.GetWebRoot() + "/" + req["filePath"];
                folder = folder.Remove(folder.LastIndexOf("/") + 1);
                System.IO.Directory.CreateDirectory(folder);

                if (req.TryGetValue("json",out _))
                {
                    var jsonPath = App.GetWebRoot() + "/" + req["filePath"] + ".json";
                    System.IO.File.WriteAllText(jsonPath, req["json"].ToString(), Encoding.UTF8);
                }

                if (req.TryGetValue("jsx", out _))
                {
                    var jsxPath = App.GetWebRoot() + "/" + req["filePath"] + ".jsx";
                    System.IO.File.WriteAllText(jsxPath, req["jsx"].ToString(), Encoding.UTF8);
                }

                if (req.TryGetValue("sql", out _))
                {
                    var sqlPath = App.GetWebRoot() + "/" + req["filePath"] + ".sql";
                    System.IO.File.WriteAllText(sqlPath, req["sql"].ToString(), Encoding.UTF8);
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
