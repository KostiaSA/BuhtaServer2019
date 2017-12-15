using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer.Controllers
{
    public class BaseAdminController : Controller
    {
        public bool AuthOk()
        {
            return true;
            //return HttpContext.Session.GetInt32("adminauth") == 1;
        }

        public Object NoAuthResponse() {
            return new { error = "NoAdminAuth" };
        }

}
}