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
    [Route("api/admin/loadSchemaTree")]
    public class AdminLoadSchemaTreeController : BaseAdminController
    {
        class ResponseObject
        {
            public string name;
            public bool isFolder;
            public List<ResponseObject> items;
        }


        private void loadFileNames(ResponseObject obj, string path, Newtonsoft.Json.Linq.JArray objectTypes)
        {
            string[] dirs = Directory.GetDirectories(path);

            string[] files1 = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            string[] files2 = Directory.GetFiles(path, "*.test.jsx", SearchOption.TopDirectoryOnly);
            var files = files1.Union(files2).ToArray();

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
                newDirItem.isFolder = true;

                loadFileNames(newDirItem, dir, objectTypes);

                obj.items.Add(newDirItem);


            }
            foreach (var file in files)
            {
                //FileInfo info = new FileInfo(file);
                var newFileItem = new ResponseObject();
                newFileItem.name = Path.GetFileName(file);

                if (objectTypes.Count > 0)
                {
                    var words = newFileItem.name.Split(".");
                    var itemType = words[words.Length - 2]; // "Заявки.table.json"
                    if (objectTypes.ToObject<string[]>().Contains(itemType))
                        obj.items.Add(newFileItem);
                }
                else
                    obj.items.Add(newFileItem);
            }

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
                res.isFolder = true;
                loadFileNames(res, App.GetWebRoot() + "/" + req.path, req.objectTypes);
                return res;
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
