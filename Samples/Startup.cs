using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
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
            services.AddAuthorization()
                    .AddAuthorizationPolicyEvaluator()
                    .AddSingleton<IHttpRequestReader, NewtonsoftJsonRequestReader>()
                    .AddSingleton<IHttpResponseWriter, NewtonsoftJsonResponseWriter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting(routes =>
            {
                routes.MapRouteProviders<Startup>();
                // routes.MapHttpHandler<MyHandler>();
            });

            app.UseAuthorization();
        }
    }
}