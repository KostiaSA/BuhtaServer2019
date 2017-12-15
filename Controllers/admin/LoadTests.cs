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
    [Route("api/admin/loadTests")]
    public class AdminLoadTestsController : BaseAdminController
    {
        class ResponseObject
        {
            public string name;
            public List<ResponseObject> items;
        }


        private bool loadFileNames(ResponseObject obj, string path)
        {
            var result = false;
            string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path, "*.test.jsx", SearchOption.TopDirectoryOnly);
            if (dirs.Length > 0 || files.Length > 0)
            {
                obj.items = new List<ResponseObject>();
            }
            foreach (var dir in dirs)
            {
                var newItemName = Path.GetFileName(dir);
                if (obj.name == "root" && newItemName == "vendor")
                    continue;
                var newDirItem = new ResponseObject();
                newDirItem.name = newItemName;



                var res = loadFileNames(newDirItem, dir);
                if (res)
                    obj.items.Add(newDirItem);

                result = result || res;


            }
            foreach (var file in files)
            {
                //FileInfo info = new FileInfo(file);
                var newFileItem = new ResponseObject();
                newFileItem.name = Path.GetFileName(file);

                obj.items.Add(newFileItem);
                result = true;

            }
            return result;

        }   //Newtonsoft.Json.Linq.JArray


        [HttpPost]
        public object Post([FromBody]dynamic req)
        {
            try
            {
                if (!AuthOk())
                    return NoAuthResponse();

                var res = new ResponseObject();
                res.name = "root";
                loadFileNames(res, App.GetWebRoot() + "/" + req.path);
                return res;
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
