using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Web.Framework;

namespace Samples
{
    // Port of the social sample here
    // https://github.com/aspnet/Security/blob/bf685de16be9949d67e93cc058ef4393f005756b/samples/SocialSample/Startup.cs
    public class SocialHandler
    {
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public SocialHandler(IAuthenticationSchemeProvider schemeProvider)
        {
            _schemeProvider = schemeProvider;
        }

        [Authorize]
        public async Task Get(HttpContext context)
        {
            // Display user information
            var response = context.Response;
            response.ContentType = "text/html";
            await response.WriteAsync("<html><body>");
            await response.WriteAsync($"Hello {(context.User.Identity.Name ?? "anonymous")}<br>");
            foreach (var claim in context.User.Claims)
            {
                await response.WriteAsync(claim.Type + ": " + claim.Value + "<br>");
            }

            await response.WriteAsync("Tokens:<br>");

            await response.WriteAsync($"Access Token:{await context.GetTokenAsync("access_token")}<br>");
            await response.WriteAsync($"Refresh Token: {await context.GetTokenAsync("refresh_token")}<br>");
            await response.WriteAsync($"Token Type: {await context.GetTokenAsync("token_type")}<br>");
            await response.WriteAsync($"expires_at: {await context.GetTokenAsync("expires_at")}<br>");
            await response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
            await response.WriteAsync("<a href=\"/refresh_token\">Refresh Token</a><br>");
            await response.WriteAsync("</body></html>");
        }

        [HttpGet("/login")]
        public async Task Login(HttpContext context, [FromQuery]string authScheme)
        {
            if (!string.IsNullOrEmpty(authScheme))
            {
                // By default the client will be redirect back to the URL that issued the challenge (/login?authScheme=foo),
                // send them to the home page instead (/).
                await context.ChallengeAsync(authScheme, new AuthenticationProperties() { RedirectUri = "/" });
                return;
            }

            var response = context.Response;
            response.ContentType = "text/html";
            await response.WriteAsync("<html><body>");
            await response.WriteAsync("Choose an authentication scheme: <br>");
            foreach (var provider in await _schemeProvider.GetAllSchemesAsync())
            {
                await response.WriteAsync($"<a href=\"?authscheme={provider.Name}\">{(provider.DisplayName ?? "(suppressed)")}</a><br>");
            }
            await response.WriteAsync("</body></html>");
        }

        [HttpGet("/logout")]
        public async Task Logout(HttpContext context)
        {
            var response = context.Response;
            response.ContentType = "text/html";
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await response.WriteAsync("<html><body>");
            await response.WriteAsync($"You have been logged out. Goodbye {context.User.Identity.Name}<br>");
            await response.WriteAsync("<a href=\"/\">Home</a>");
            await response.WriteAsync("</body></html>");
        }

        [HttpGet("/error")]
        public async Task Error(HttpContext context, [FromQuery]string failureMessage)
        {
            var response = context.Response;
            response.ContentType = "text/html";
            await response.WriteAsync("<html><body>");
            await response.WriteAsync($"An remote failure has occurred: {failureMessage}<br>");
            await response.WriteAsync("<a href=\"/\">Home</a>");
            await response.WriteAsync("</body></html>");
        }
    }
}
