using Blazored.Modal;

using Markdig;
using Markdig.Extensions.AutoIdentifiers;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MikeCodesDotNET.Data;
using MikeCodesDotNET.Hubs;
using MikeCodesDotNET.Services;
using MikeCodesDotNET.Services.Blog;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Tewr.Blazor.FileReader;

using Westwind.AspNetCore.Markdown;


namespace MikeCodesDotNET
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

#if !DEBUG
           services.AddApplicationInsightsTelemetry();
#endif

            services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddDbContextFactory<ApplicationDbContext>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddFileReaderService();
            services.AddBlazoredModal();

            services.AddSingleton<SentimentService>();

            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<BlogPostStorageService>();
            services.AddScoped<AzureSearchService>();


            services.AddHttpClient();
            services.AddScoped<HttpClient>();
            services.AddScoped<ImageService>();

           

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add authentication services
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect("Auth0", options =>
            {
                // Set the authority to your Auth0 domain
                options.Authority = $"https://{Configuration["Auth0:Domain"]}";

                // Configure the Auth0 Client ID and Client Secret
                options.ClientId = Configuration["Auth0:ClientId"];
                options.ClientSecret = Configuration["Auth0:ClientSecret"];

                // Set response type to code
                options.ResponseType = "code";

                // Configure the scope
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");

                // Set the callback path, so Auth0 will call back to http://localhost:3000/callback
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
                options.CallbackPath = new PathString("/callback");

                // Configure the Claims Issuer to be Auth0
                options.ClaimsIssuer = "Auth0";

                options.Events = new OpenIdConnectEvents
                {
                    // handle the logout redirection
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                          {
                              string logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

                              string postLogoutUri = context.Properties.RedirectUri;
                              if (!string.IsNullOrEmpty(postLogoutUri))
                              {
                                  if (postLogoutUri.StartsWith("/"))
                                  {
                                      // transform to absolute
                                      HttpRequest request = context.Request;
                                      postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                                  }
                                  logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                              }

                              context.Response.Redirect(logoutUri);
                              context.HandleResponse();

                              return Task.CompletedTask;
                          }
                };
            });

            services.AddHttpContextAccessor();
            services.AddBlazorContextMenu(options =>
            {
                options.ConfigureTemplate("myTemplate", template =>
                {
                    template.MenuCssClass = "my-menu";
                    template.MenuItemCssClass = "my-menu-item";
                    template.SeperatorCssClass = "my-menu-separator";
                    template.SeperatorHrCssClass = "my-menu-hr";
                });
            });

            services.AddMarkdown(config =>
            {
                // optional Tag BlackList
                config.HtmlTagBlackList = "script|iframe|object|embed|form"; // default

                // Simplest: Use all default settings
                MarkdownProcessingFolder folderConfig = config.AddMarkdownProcessingFolder("/docs/", "~/Pages/__MarkdownPageTemplate.cshtml");

                // Customized Configuration: Set FolderConfiguration options
                folderConfig = config.AddMarkdownProcessingFolder("/posts/", "~/Pages/__MarkdownPageTemplate.cshtml");

                // Optionally strip script/iframe/form/object/embed tags ++
                folderConfig.SanitizeHtml = false;  //  default

                // Optional configuration settings
                folderConfig.ProcessExtensionlessUrls = true;  // default
                folderConfig.ProcessMdFiles = true; // default

                // Optional pre-processing - with filled model
                folderConfig.PreProcess = (model, controller) =>
                {
                    // controller.ViewBag.Model = new MyCustomModel();
                };

                // Create your own IMarkdownParserFactory and IMarkdownParser implementation
                // to replace the default Markdown Processing
                //config.MarkdownParserFactory = new CustomMarkdownParserFactory();                 

                // optional custom MarkdigPipeline (using MarkDig; for extension methods)
                config.ConfigureMarkdigPipeline = builder =>
                {
                    builder.UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Default)
                        .UsePipeTables()
                        .UseGridTables()
                        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // Headers get id="name" 
                        .UseAutoLinks() // URLs are parsed into anchors
                        .UseAbbreviations()
                        .UseYamlFrontMatter()
                        .UseEmojiAndSmiley(true)
                        .UseListExtras()
                        .UseFigures()
                        .UseTaskLists()
                        .UseBootstrap()
                        //.DisableHtml()   // renders HTML tags as text including script
                        .UseGenericAttributes();
                };
            });


            services.AddDatabaseDeveloperPageExceptionFilter();
            // services.AddScoped<IBlogPostsLoaderService, FileSystemBlogService>();

            services.AddScoped<IAlertService, AlertService>();
            services.AddScoped<IBrowserStorageService, BrowserStorageService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseMvcWithDefaultRoute();
            app.UseMarkdown();

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();



            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapHub<GridEventsHub>("/hubs/gridevents");
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
