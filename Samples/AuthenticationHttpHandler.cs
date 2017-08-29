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
    public class AuthenticationHttpHandler : HttpHandler
    {
        private readonly AuthenticationOptions _options;

        public AuthenticationHttpHandler(IOptions<AuthenticationOptions> options)
        {
            _options = options.Value;
        }

        public async Task<Result> InvokeAsync(HttpContext context)
        {
            var result = await context.AuthenticateAsync(_options.DefaultAuthenticateScheme);

            if (result.Succeeded)
            {
                context.User = result.Principal;
            }

            return Next();
        }
    }
}
