namespace Gmail_Service
{
    using Google.Apis.Gmail.v1;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Authentication.Cookies;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddMvc();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Google";
            })
        .AddCookie()
        .AddGoogle("Google", options =>
        {
            options.ClientId = "693216272392-3086bfp74k0ri3uq88nd5qqh1t2q9m8a.apps.googleusercontent.com";
            options.ClientSecret = "KRJmeMsqNZCEGQd9HMYSO_Qe";
            options.CallbackPath = new Microsoft.AspNetCore.Http.PathString("/signin-google");
            options.Scope.Add(GmailService.Scope.MailGoogleCom);
            options.Scope.Add(GmailService.Scope.GmailReadonly);
            options.Scope.Add(GmailService.Scope.GmailCompose);
            options.SaveTokens = true;
        });
            services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            else { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            { endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); });
        }
    }
}
