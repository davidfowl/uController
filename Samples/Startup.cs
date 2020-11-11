using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using uController;

namespace Samples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHttpHandler<MyHandler>();
                endpoints.MapHttpHandler<ProductsHandler>();
            });
        }
    }

    public class ProductsHandlerFactory : Factory<ProductsHandler>
    {
        public override ProductsHandler Create()
        {
            return new ProductsHandler();
        }
    }

    public class MyHandlerFactory : Factory<MyHandler>
    {
        public override MyHandler Create()
        {
            return new MyHandler();
        }
    }

    public abstract class Factory<T> where T : HttpHandler
    {
        public abstract T Create();
    }
}