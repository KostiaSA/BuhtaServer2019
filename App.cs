using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer
{
    public static class App
    {
        public static string GetWebRoot()
        {
            return @"c:\$\Buhta2019\BuhtaClient\wwwroot";
        }

        public static int GetPort()
        {
            return 443;
        }

        public static string GetUrls()
        {
            return "https://localhost:" + GetPort();
        }
    }
}
