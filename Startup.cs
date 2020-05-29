using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using B2CDeviceCode.Models;
using B2CDeviceCode.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                .AddScoped<IConfidentialClientApplication>((svcProvider) =>
                {
                    var opts = new ConfidentialClientApplicationOptions();
                    Configuration.Bind("MSAL", opts);
                    var AuthorityBase = $"https://mrochonb2cprod.b2clogin.com/tfp/mrochonb2cprod.onmicrosoft.com/";
                    var Authority = $"{AuthorityBase}B2C_1_BasicSUSI";
                    return ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(opts)
                        .WithB2CAuthority(Authority)
                        .Build();
                })
                .AddAuthentication(options =>
                {
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
                        //options.Scope.Add("offline_access"); // otherwise no refresh token or acct created in cache
                        options.Events.OnRedirectToIdentityProvider = async (ctx) =>
                        {
                            var request = (RequestStatus)ctx.Properties.Parameters["request"];
                            //ctx.ProtocolMessage.ClientId = options.ClientId = request.client_id;
                            options.Scope.Clear();
                            foreach (var scope in request.scopes)
                                options.Scope.Add(scope);
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
                            var oauth2 = ctx.HttpContext.RequestServices.GetService<IConfidentialClientApplication>();
                            status.authResult = await oauth2.AcquireTokenByAuthorizationCode(status.scopes, ctx.ProtocolMessage.Code).ExecuteAsync();
                            status.isReady = true;
                            await db.StringSetAsync(userCode, JsonConvert.SerializeObject(status), data.Expiry, When.Exists);
                            ctx.HandleCodeRedemption(ctx.ProtocolMessage.IdToken, ctx.ProtocolMessage.IdToken);
                        };
                    });
                //.AddAuthentication(AzureADB2CDefaults.OpenIdScheme)
                //    .AddAzureADB2C(options => Configuration.Bind("AzureADB2C", options));

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
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
