using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;

namespace BuhtaServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.SslPort = App.GetPort();
                options.Filters.Add(new RequireHttpsAttribute());
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });


            services.AddAntiforgery(
                    options =>
                    {
                        options.Cookie.Name = "_af";
                        options.Cookie.HttpOnly = true;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                        options.HeaderName = "X-XSRF-TOKEN";
                    }
                );
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromSeconds(600);
                options.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            };

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.ToString().StartsWith("/ws/"))
                {
                    string paramStr = context.Request.Path.ToString().Substring(4);
                    string userSesionId = paramStr.Split("/")[0];
                    string userWindowId = paramStr.Split("/")[1];

                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                        UserSession UserSession;
                        var sessionOk = Auth.UserSessions.TryGetValue(new Guid(userSesionId), out UserSession);

                        if (!sessionOk)
                            throw new Exception("нет авторизации");

                        UserSession.webSockets.AddOrUpdate(userWindowId, webSocket, (key, oldValue) => webSocket);


                        //var result = ClientWebSocket.ClientWebSockets.TryAdd(userSesionId, webSocket);
                        //if (!result)
                        //    throw new Exception("ClientWebSocket.ClientWebSockets.TryAdd?");

                        Console.WriteLine("WebSocket webSocket");
                        var buffer = new byte[1024 * 4];
                        WebSocketReceiveResult readResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        while (!readResult.CloseStatus.HasValue)
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, readResult.Count), readResult.MessageType, readResult.EndOfMessage, CancellationToken.None);

                            readResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }
                        //await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        //await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
            app.UseSession();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
