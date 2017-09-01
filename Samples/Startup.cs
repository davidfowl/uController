using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Samples;
using Web.Framework;

namespace Samples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication("Cookies")
                    .AddCookie();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpHandler<AuthenticationHttpHandler>();

            app.UseHttpHandler<Handler>(model =>
            {
                model.Method(nameof(Handler.GetAll))
                     .Get("/products");

                model.Method(nameof(Handler.Get))
                     .Get("/products/{id}")
                     .FromRoute("id");

                model.Method(nameof(Handler.Post))
                     .Post("/products")
                     .FromBody("product");

                model.Method(nameof(Handler.Delete))
                     .Delete("/products/{id}")
                     .FromRoute("id");
            });

            app.UseHttpHandler<MyHandler>();
        }
    }
}
