using ApiGateway.Interfaces;
using ApiGateway.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApiGateway.Models;
using System;
using System.Text;
using Docker.DotNet;

namespace ApiGateway
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
            var rabbitMqManagementOpts = Configuration.GetSection(RabbitMqManagementOptions.Position)
                .Get<RabbitMqManagementOptions>();

            services.AddHttpClient<IRabbitMqManagementService, RabbitMqManagementService>(c =>
            {
                c.BaseAddress = new Uri(rabbitMqManagementOpts.BaseAddress);
                var authString = $"{rabbitMqManagementOpts.Username}:{rabbitMqManagementOpts.Password}";
                var base64EncAuthString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));
                c.DefaultRequestHeaders.Add("Authorization", $"Basic {base64EncAuthString}");
            });

            services.AddHttpClient<ILogMessageService, LogMessageService>(c =>
            {
                c.BaseAddress = new Uri(Configuration.GetValue<string>("HttpServ:Uri"));
            });

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSeq(Configuration.GetSection("Seq"));
            });

            // Docker engine API Ver: 1.40 at vagrant and CI server
            services.AddSingleton<IDockerClient>(provider =>
            {
                var socket = Configuration.GetValue<string>("DockerApi:SocketPath");
                var config = new DockerClientConfiguration(new Uri(socket), new AnonymousCredentials());
                return config.CreateClient(new Version(1, 40));
            });

            services.AddSingleton(typeof(IDockerHostService), typeof(DockerHostService));
            services.AddSingleton(typeof(IStateService), typeof(StateService));
            
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
