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
    [Route("api/admin/generateSqlFromTemplate")]
    public class AdminGenerateSqlFromTemplateController : BaseAdminController
    {
        class ResponseObject
        {
            public string sql;
        }

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {


            try
            {
                if (!AuthOk())
                    return NoAuthResponse();

                    return new { sql = SqlTemplate.emitSqlBatchFromTemplateText(req.dialect.ToString(), req.sqlTemplate.ToString(), JObject.Parse(req.paramsObj.ToString()), HttpContext, Request) };

            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
