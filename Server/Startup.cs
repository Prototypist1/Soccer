using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(x=> {
#if DEGBU
                x.EnableDetailedErrors = true;
#endif
            }).AddMessagePackProtocol();//.AddAzureSignalR();//;//
            services.AddSingleton<GameHubState>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //UseSignalR
            //.UseAzureSignalR
            app.UseSignalR(routes =>
            {
                routes.MapHub<GameHub>($"/{nameof(GameHub)}", x=> {
                    x.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                });
            });



            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
