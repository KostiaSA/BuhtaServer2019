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

                if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                    return NoAuthResponse();

                //                return new { sql = SqlTemplate.emitSqlBatchFromTemplateText(req.dialect.ToString(), req.sqlTemplate.ToString(), JObject.Parse(req.paramsObj.ToString()), HttpContext, Request) };
                return new { sql = SqlTemplate.emitSqlBatchFromTemplateText(request["dialect"].ToString(), request["sqlTemplate"].ToString(), (JObject)request["paramsObj"], HttpContext, Request) };

            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
