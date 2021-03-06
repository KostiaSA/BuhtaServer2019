﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using BuhtaServer.test;

namespace BuhtaServer
{
    public class Program
    {
        public static BuhtaConfig BuhtaConfig;


        public static void Main(string[] args)
        {
            //TestHandlebars.test1();
            SqlTemplate.InitSqlFilesWatcher();
            BuildWebHost(args).Run();
        }

        public static int startupErrorCount = 0;

        public static void RegisterStartupError(string error)
        {
            startupErrorCount++;
            throw new Exception(error);
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                       .SetBasePath(Directory.GetCurrentDirectory())
                       .AddEnvironmentVariables()
                       .AddJsonFile("certificate.json", optional: true, reloadOnChange: true)
                       .AddJsonFile($"certificate.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                       .AddJsonFile("buhtaSettings.json", optional: false, reloadOnChange: false)
                       .Build();

            var certificateSettings = config.GetSection("certificateSettings");
            string certificateFileName = certificateSettings.GetValue<string>("filename");
            string certificatePassword = certificateSettings.GetValue<string>("password");

            var certificate = new X509Certificate2(certificateFileName, certificatePassword);

            BuhtaConfig = JsonConvert.DeserializeObject<BuhtaConfig>(File.ReadAllText("buhtaSettings.json"));
            BuhtaConfig.CheckOnStartup();


            // проверяем 


            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    //options.Listen(IPAddress.Loopback, App.GetPort(), listenOptions =>
                    //{
                    //    listenOptions.UseHttps(certificate);
                    //});
                    options.Listen(IPAddress.Any, App.GetPort(), listenOptions =>
                    {
                        listenOptions.UseHttps(new X509Certificate2("buhta2019.pfx", "buhta123"));
                    });
                    options.Listen(IPAddress.Any, 4439, listenOptions =>
                    {
                        listenOptions.UseHttps(new X509Certificate2("buhta2019.pfx", "buhta123"));
                    });
                })
                .UseWebRoot(App.GetWebRoot())
                //.UseUrls(App.GetUrls(),"http://127.0.0.1:8080")
                .UseStartup<Startup>()
                .Build();
        }
    }
}
