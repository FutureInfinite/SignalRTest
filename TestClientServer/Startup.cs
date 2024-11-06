using Microsoft.AspNetCore.Http.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClientServer
{
    internal class Startup
    {
        #region Properties&Attributes
        public static string ListenPort { get; private set; }
        public static string TargetURL { get; private set; }
        private static string PolicyName { get { return "Trading Client Policy"; } }
        #endregion Properties&Attributes

        #region Lifetime
        static Startup()
        {
#if DEBUG
            ListenPort = "20";
            Console.WriteLine(string.Format("HUB URL {0}", ListenPort));
#else
            ListenPort = System.Configuration.ConfigurationManager.AppSettings["TradeClientSignalRURL"];
#endif

            TargetURL = string.Format("http://localhost:{0}", ListenPort);
            Console.WriteLine(string.Format("orderIT HUB listenining on {0}", TargetURL));
        }

        #endregion Lifetime

        #region Operations   
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();

            services.AddCors(o =>
            {
                o.AddDefaultPolicy(b =>
                {
                    b.WithOrigins(TargetURL)
                        .AllowCredentials()
                        .AllowAnyHeader();
                });
            });

            //services.AddHostedService<Worker>();

            services.AddSignalR(hubOptions =>
            {
                //hubOptions.EnableDetailedErrors = true;
                hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(100);
                //hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            });
            // Register MessageBackgroundService
            services.AddHostedService<MessageBackgroundService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseSwagger();
                //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "oiStockPusherSite v1"));
            }

            app.UseRouting();
            app.UseCors(PolicyName);

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers(); 
                endpoints.MapHub<TheHub>("/TestHub", options =>
                {
                    options.Transports = HttpTransportType.LongPolling; // you may also need this
                });
            });
        }

        #endregion Operations
    }
}