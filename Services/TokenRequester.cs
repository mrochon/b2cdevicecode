using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CDeviceCode.Services
{
    public class TokenRequester
    {
        public TokenRequester(IHttpContextAccessor ctx, IConfidentialClientApplication oauth2Client)
        {
            _ctx = ctx;
            _oauth2Client = oauth2Client;
        }
        IConfidentialClientApplication _oauth2Client;
        IHttpContextAccessor _ctx;
        public async Task<string> GetUserAccessTokenAsync(string[] scopes)
        {
            return null;
        }
        public async Task<string> GetAppAccessTokenAsync(string[] scopes)
        {
            return null;
        }
    }
}
