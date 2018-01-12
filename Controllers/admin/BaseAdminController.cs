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
        protected bool AuthOk()
        {
            Console.WriteLine(HttpContext.Session.Id);
            return true;
            //return HttpContext.Session.GetInt32("adminauth") == 1;
        }

        protected Object NoAuthResponse() {
            return new { error = "NoAdminAuth" };
        }

}
}