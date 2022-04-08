using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MikeCodesDotNET.Data;
using MikeCodesDotNET.Services.Blog;

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MikeCodesDotNET
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // this section sets up and seeds the database. It would NOT normally
            // be done this way in production. It is here to make the sample easier,
            // i.e. clone, set connection string and run.
            var serviceProvider = host.Services.GetService<IServiceScopeFactory>()
                .CreateScope()
                .ServiceProvider;
            var _ = serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            await EnsureDbCreatedAsync(serviceProvider.GetRequiredService<IWebHostEnvironment>(), serviceProvider.GetRequiredService<BlogPostStorageService>());

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true); // optional extra provider

                    if (env.IsDevelopment()) // different providers in dev
                    {
                        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                        if (appAssembly != null)
                        {
                            config.AddUserSecrets(appAssembly, optional: true);
                        }
                    }

                    config.AddEnvironmentVariables(); // overwrites previous values

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        private static async Task EnsureDbCreatedAsync(IWebHostEnvironment environment, BlogPostStorageService blogPostImportService)
        {


            // empty to avoid logging while inserting (otherwise will flood console)
            var factory = new LoggerFactory();
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseLoggerFactory(factory);                       

            using var context = new ApplicationDbContext(environment);

            //await context.Database.EnsureDeletedAsync();

            // result is true if the database had to be created
            if (await context.Database.EnsureCreatedAsync())
            {
                Debug.WriteLine("Created Database");
            }
        }
    }
}
