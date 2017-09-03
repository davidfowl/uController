using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

            // Example 1
            // - Routes and http methods are mapped per method
            // - [FromRoute] parameters are explicitly declared
            // - [FromBody] paramters are explictly declared
            app.UseHttpHandler<ProductsApi>(model =>
            {
                model.Method(nameof(ProductsApi.GetAll))
                     .Get("/products");

                model.Method(nameof(ProductsApi.Get))
                     .Get("/products/{id}")
                     .FromRoute("id");

                model.Method(nameof(ProductsApi.Post))
                     .Post("/products")
                     .FromBody("product");

                model.Method(nameof(ProductsApi.Delete))
                     .Delete("/products/{id}")
                     .FromRoute("id");
            });

            // Example2
            // - Routes in startup
            // - method names map to http verbs
            // - route paramters are automatically bound by name
            // - [FromBody] paramters are explictly declared
            app.UseHttpHandler<ProductsApi2>(model =>
            {
                model.Method(nameof(ProductsApi.GetAll))
                     .Route("/products2");

                model.Method(nameof(ProductsApi.Get))
                     .Route("/products2/{id}");

                model.Method(nameof(ProductsApi.Post))
                     .Route("/products2")
                     .FromBody("product");

                model.Method(nameof(ProductsApi.Delete))
                     .Route("/products2/{id}");

                model.MapMethodNamesToHttpMethods();
                model.MapRouteParametersToMethodArguments();
            });

            // Example 3
            // - Routes on the class itself
            // - method names map to http verbs
            // - route paramters are automatically bound by name
            // - complex types are automatically [FromBody]
            app.UseHttpHandler<ProductsApi3>(model =>
            {
                model.MapMethodNamesToHttpMethods();
                model.MapRouteParametersToMethodArguments();
                model.MapComplexTypeArgsToFromBody();
            });

            app.UseHttpHandler<MyHandler>();
        }
    }
}
