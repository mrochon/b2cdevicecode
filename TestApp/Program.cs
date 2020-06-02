using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static string clientId = "f0df2619-4370-4f7e-8a77-b6a97fe2cabc";
        static string redirectUri = "app://devicecode";
        static string[] scopes = { "https://mrochonb2cprod.onmicrosoft.com/webapi/read_policies" };

        static void Main(string[] args)
        {
            Console.WriteLine("Get an OAuth2 access token using the Device Code grant using Azure B2C...");
            (new Program()).GetAccessToken().Wait();
        }
        public async Task GetAccessToken()
        {
            var oauth2 = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAdfsAuthority("https://localhost:44358/adfs", false)
                .WithExtraQueryParameters("p=B2C_1_BasicSUSI")
                .WithRedirectUri(redirectUri)
                .Build();

            var authResult = await oauth2.AcquireTokenWithDeviceCode(scopes, async (res) => 
            {
                Console.WriteLine(res.Message);
                Console.WriteLine($"Url: {res.VerificationUrl}");
                Console.WriteLine($"Device code: {res.UserCode}");
                await Task.FromResult(0); 
            }).ExecuteAsync().ConfigureAwait(false);

            Console.WriteLine(authResult.AccessToken);
        }
    }
}
