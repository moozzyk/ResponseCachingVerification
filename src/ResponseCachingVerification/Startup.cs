﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ResponseCachingVerification
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryResponseCacheStore();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseResponseCache();
            app.UseFileServer();

            app.Run(async (context) =>
            {
                foreach(var q in context.Request.Query)
                {
                    context.Response.Headers[q.Key] = q.Value;
                }
                // context.Request.Query
                // context.Response.Headers["Cache-Control"] = "public";
                context.Response.Headers["X-my-time"] = DateTime.Now.ToString("o");

                if (context.Request.Method != "HEAD")
                {
                    await context.Response.WriteAsync("Hello World!\n" + DateTime.Now.ToString("o"));
                }
            });
        }
    }
}
