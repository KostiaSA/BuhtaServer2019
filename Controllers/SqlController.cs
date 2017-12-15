using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuhtaServer.Controllers
{
    [Produces("application/json")]
    [Route("api/sql")]
    public class SqlController : Controller
    {
        // GET: api/sql
        [HttpGet]
        public string Get()
        {
            //return "это get ответ:";
            return Post("тестовый get");
        }

        // POST: api/Sql
        [HttpPost]
        public string Post([FromBody]string value)
        {
            return "это post ответ: "+value;
        }
        
    }

//    [Produces("application/json")]
//    [Route("api/admin/loadSchemaObjectFiles")]
//    public class AdminLoadSchemaObjectFilesController : Controller
//    {

//        // POST: api/Sql
//        [HttpPost]
//        public object Post([FromBody]dynamic req)
//        {
////            throw new Exception("жопа?");
//            return new { error = "жопа?" };
//            //  return new { jsx = "ConsoleColor.Yellow", json = "ConsoleColor.Red" + req.filePath };
//        }

//    }
}
