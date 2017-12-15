using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

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
                if (!AuthOk())
                    return NoAuthResponse();

                var fullPath = App.GetWebRoot() + "/" + req.filePath;
                var res = new ResponseObject();
                if (System.IO.File.Exists(fullPath))
                {
                    res.code = System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
                    return res;

                }
                else
                    return new { error = "не найден файл '"+ req.filePath + ".json (или .jsx)'" };
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
