using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

                // Works!
                //endpoints.MapAction("/api/products", () =>
                //{
                //    return Enumerable.Empty<Product>();
                //});

                //endpoints.MapAction("/api/products/2", () =>
                //{
                //    return Enumerable.Empty<Product>();
                //});

                //endpoints.MapAction("/api/products/{id}", (int? id) =>
                //{
                //    if (id is null)
                //    {
                //        return null;
                //    }

                //    return new Product(0, "Name", 1.4);
                //});

                //endpoints.MapAction("/api/products", (Product product) =>
                //{
                //    return product;
                //});
            });
        }
    }
}