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

            // Full external metadata
            app.UseHttpHandler<ProductsApi>(model =>
            {
                model.Method(nameof(ProductsApi.GetAll))
                     .Route("/products");

                model.Method(nameof(ProductsApi.Get))
                     .Route("/products/{id}");

                model.Method(nameof(ProductsApi.Post))
                     .Route("/products")
                     .FromBody("product");

                model.Method(nameof(ProductsApi.Delete))
                     .Route("/products/{id}");

                model.MapMethodNamesToHttpMethods();

                // Automatically map route parameters to method arguments with a matching name
                model.MapRouteParametersToMethodArguments();
            });

            // Routes are in attributes here
            app.UseHttpHandler<ProductsApi2>(model =>
            {
                model.MapMethodNamesToHttpMethods();
                model.MapRouteParametersToMethodArguments();
                model.MapComplexTypeArgsToFromBody();
            });

            app.UseHttpHandler<MyHandler>();
        }
    }
}
