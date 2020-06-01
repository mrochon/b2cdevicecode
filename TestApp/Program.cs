using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static string clientId = "f0df2619-4370-4f7e-8a77-b6a97fe2cabc";
        static string RedirectUri = "app://devicecode";
        static string[] Scopes = { "https://mrochonb2cprod.onmicrosoft.com/webapi/read_policies" };

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            (new Program()).GetAccessToken().Wait();
        }
        public async Task GetAccessToken()
        {
            var oauth2 = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAdfsAuthority("https://localhost:44358/adfs", false)
                .WithExtraQueryParameters("p=B2C_1_BasicSUSI")
                .WithRedirectUri(RedirectUri)
                .Build();

            var authResult = await oauth2.AcquireTokenWithDeviceCode(Scopes, async (res) => 
            {
                Console.WriteLine(res.Message);
                Console.WriteLine(res.VerificationUrl);
                Console.WriteLine(res.UserCode);
                await Task.FromResult(0); 
            }).ExecuteAsync().ConfigureAwait(false);

            Console.WriteLine(authResult.AccessToken);
        }
    }
}
