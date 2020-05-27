using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using B2CDeviceCode.Models;
using B2CDeviceCode.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace B2CDeviceCode.Controllers
{
    [Route("adfs/[controller]")]
    [ApiController]
    public class oauth2Controller : ControllerBase
    {
        public oauth2Controller(ConnectionMultiplexer redis, IDataProtectionProvider crypto, IOptions<IssuanceOptions> options)
        {
            _redis = redis;
            _crypto = crypto.CreateProtector("request");
            _options = options;
        }
        ConnectionMultiplexer _redis;
        IDataProtector _crypto;
        IOptions<IssuanceOptions> _options;

        [HttpPost("devicecode")]
        //[Route("devicecode")]
        public async Task<ActionResult> devicecode([FromQuery] string p, [FromForm] string client_id, [FromForm] string scope)
        {
            var scopeList = scope.Split(' ');
            var db = _redis.GetDatabase();
            var requestStatus = new RequestStatus()
            {
                client_id = client_id,
                scopes = scopeList,
                isReady = false,
                journeyName = p,
            };
            bool saved;
            do
            {
                //TODO: use DI for crypto
                requestStatus.userCode = RandomString(6);
                saved = await db.StringSetAsync(requestStatus.userCode, JsonConvert.SerializeObject(requestStatus), TimeSpan.FromMinutes(5), When.NotExists);
            } while (!saved);
            var url = this.Request.Path;
            return new JsonResult(new
            {
                user_code = requestStatus.userCode,
                expires_in = _options.Value.ExpiresInSeconds,
                interval = _options.Value.RequestInterval,
                message = _options.Value.Message,
                device_code = _crypto.Protect(JsonConvert.SerializeObject(requestStatus)),
                verification_url = $"{Request.Scheme}://{Request.Host.Value}/devicelogin"
            });
        }
        [HttpPost("token")]
        //[Route("token")]
        public async Task<ActionResult> token([FromQuery] string journey, [FromForm] TokenRequest request)
        {
            var codeValue = JsonConvert.DeserializeObject<RequestStatus>(_crypto.Unprotect(request.device_code));
            var db = _redis.GetDatabase();
            var resp = await db.StringGetAsync(codeValue.userCode);
            var badResult = new
            {
                correlation_id = 1,
                error = "authorization_pending",
                error_code = new int[] { 70016 },
            };
            if (resp.IsNullOrEmpty)
                return new BadRequestObjectResult(badResult);
            var status = JsonConvert.DeserializeObject<RequestStatus>(resp);
            if (!status.isReady)
                return new BadRequestObjectResult(badResult);
            return new JsonResult(status.authResult);
        }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());

        }
    }
}