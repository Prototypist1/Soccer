using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(x =>
            {
#if DEBUG
                x.EnableDetailedErrors = true;
#endif
            })
            //.AddAzureSignalR()
            .AddMessagePackProtocol()
            ;
            services.AddSingleton<GameHubState>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //UseSignalR
            //.UseAzureSignalR
            app
                .UseSignalR(routes =>
                //.UseAzureSignalR(routes =>
            {
                routes.MapHub<GameHub>($"/{nameof(GameHub)}");
            });

            //app.UseRouting();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapHub<GameHub>($"/{nameof(GameHub)}");
            //});

            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});
        }
    }
}
