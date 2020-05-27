using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using B2CDeviceCode.Models;
using B2CDeviceCode.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace B2CDeviceCode.Controllers
{
    public class deviceLoginController : Controller
    {
        public deviceLoginController(ConnectionMultiplexer redis, IDataProtectionProvider crypto)
        {
            _redis = redis;
            _crypto = crypto.CreateProtector("request");
        }
        ConnectionMultiplexer _redis;
        IDataProtector _crypto;
        // GET: deviceCode
        public ActionResult Index()
        {
            return View(new UserCodeEntry());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index([Bind("userCode")] UserCodeEntry entry)
        {
            try
            {
                /*var db = _redis.GetDatabase();
                var statusStr = await db.StringGetAsync(entry.userCode);
                if (statusStr.IsNull)
                    return new NotFoundResult();*/
                //TODO: verify request is the same as original?
                var statusStr = "{\"client_id\":\"f0df2619-4370-4f7e-8a77-b6a97fe2cabc\",\"scopes\":[\"abc\",\"offline_access\",\"profile\",\"openid\"],\"userCode\":\"OA871Q\",\"journeyName\":\"journey\",\"authResult\":null,\"isReady\":false}";
                var status = JsonConvert.DeserializeObject<RequestStatus>(statusStr);
                var authParms = new AuthenticationProperties() 
                { 
                    RedirectUri = "/deviceAuth/Result" 
                };
                authParms.Parameters.Add("request", status);
                authParms.Items.Add("userCode", entry.userCode);
                return Challenge(authParms,
                    new string[] { AzureADB2CDefaults.AuthenticationScheme });
            }
            catch
            {
                return View();
            }
        }
        public ActionResult Result()
        {
            return View();
        }
    }
}