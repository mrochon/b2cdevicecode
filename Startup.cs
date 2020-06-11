using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using B2CDeviceCode.Models;
using B2CDeviceCode.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace B2CDeviceCode
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection();
            services
                .AddHttpContextAccessor()
                .AddSingleton<ConnectionMultiplexer>((svc) =>  // Redis
                {
                    var opts = new ConfigurationOptions();
                    Configuration.Bind("Redis", opts);
                    opts.EndPoints.Add("TokenRequests.redis.cache.windows.net:6380");
                    return ConnectionMultiplexer.Connect(opts);
                })
                .Configure<IssuanceOptions>(options => Configuration.GetSection("IssuanceOptions").Bind(options))
                .AddAuthentication(options =>
                {
                    //options.DefaultAuthenticateScheme = AzureADB2CDefaults.AuthenticationScheme;
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/Account/Unauthorized/";
                        options.AccessDeniedPath = "/Account/Forbidden/";
                    })
                    .AddOpenIdConnect(AzureADB2CDefaults.AuthenticationScheme, options =>
                    {
                        Configuration.Bind("OIDC", options);
                        //options.ResponseType = OpenIdConnectResponseType.CodeIdToken; // using id_token disables PKCE
                        options.Events.OnRedirectToIdentityProvider = async (ctx) =>
                        {
                            var request = (RequestStatus)ctx.Properties.Parameters["request"];
                            var b2cName = options.MetadataAddress.Split('/')[2].Split('.')[0];
                            ctx.ProtocolMessage.IssuerAddress = ctx.ProtocolMessage.IssuerAddress.Replace("b2c_1_basicsusi", request.journeyName);
                            ctx.ProtocolMessage.Parameters["client_id"] = request.client_id;
                            var scopes = request.scopes.Aggregate((i, j) => $"{i} {j}");
                            ctx.ProtocolMessage.Parameters["scope"] = $"{ctx.ProtocolMessage.Parameters["scope"]} {scopes}";
                            await Task.FromResult(0);
                        };
                        options.Events.OnAuthorizationCodeReceived = async ctx =>
                        {
                            ctx.HandleCodeRedemption();
                            var userCode = ctx.Properties.Items["userCode"];
                            var redis = ctx.HttpContext.RequestServices.GetService<ConnectionMultiplexer>();
                            var db = redis.GetDatabase();
                            var data = await db.StringGetWithExpiryAsync(userCode);
                            if (data.Value.IsNull)
                                throw new Exception("Request status not found");
                            var status = JsonConvert.DeserializeObject<RequestStatus>(data.Value);
                            var http = new HttpClient();
                            var props = ctx.TokenEndpointRequest.Parameters.ToList();
                            var scopes = status.scopes.Aggregate((i, j) => $"{i} {j}");
                            var tokenUrl = ctx.Options.MetadataAddress.Replace("v2.0/.well-known/openid-configuration", "oauth2/v2.0/token");
                            props.Add(new KeyValuePair<string, string>("scope", $"{scopes} offline_access openid")); // Not getting id_token breaks MSAL caching on clients side
                            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var resp = await http.PostAsync(
                                tokenUrl,
                                //$"https://mrochonb2cprod.b2clogin.com/mrochonb2cprod.onmicrosoft.com/{status.journeyName}/oauth2/v2.0/token", 
                                new FormUrlEncodedContent(props));
                            if (resp.IsSuccessStatusCode)
                            {
                                status.isReady = true;
                                status.authResult = await resp.Content.ReadAsStringAsync();
                                var rresp = await db.StringSetAsync(userCode, JsonConvert.SerializeObject(status), data.Expiry, When.Exists);
                            }
                            var req = ctx.HttpContext.Request;
                            ctx.Response.Redirect($"{req.Scheme}://{req.Host.Value}/devicelogin/Result");
                            ctx.HandleResponse();
                        }; 
                    });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=devicelogin}/{action=Index}/{id?}");
            });
        }
    }
}
