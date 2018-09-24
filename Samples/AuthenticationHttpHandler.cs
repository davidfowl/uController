using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Web.Framework;

namespace Samples
{
    public class AuthenticationMiddleware
    {
        private readonly AuthenticationOptions _options;
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next, IOptions<AuthenticationOptions> options)
        {
            _options = options.Value;
            _next = next;
        }

        public async Task<RequestDelegate> InvokeAsync(HttpContext context)
        {
            var result = await context.AuthenticateAsync(_options.DefaultAuthenticateScheme);

            if (result.Succeeded)
            {
                context.User = result.Principal;
            }

            return _next;
        }
    }
}
