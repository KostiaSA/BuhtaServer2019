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
    [Route("api/getDatabasesList")]
    public class GetDatabasesListController : BaseAdminController
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

                return new { dbList = Program.BuhtaConfig.databases.Select(db => new { name = db.Name, dialect = db.Dialect, note = db.Note }).ToArray() };

            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
