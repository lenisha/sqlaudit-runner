using System;

using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace sqlaudit_runner
{
   class Program
    {
        static void Main(string[] args)
        {

            // create service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // run app
            serviceProvider.GetService<SqlAuditor>().Run();
        }



        private static void ConfigureServices(IServiceCollection serviceCollection)
        {

            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (String.IsNullOrEmpty(env))
                env = "Production";
            
            // Set up configuration sources.
            var configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .Build();

            // add logging
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add Config
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddOptions();
            // add app
            serviceCollection.AddTransient<SqlAuditor>();     
            Console.WriteLine("\nSqlAuditor Loaded.");
        }

    }
}
