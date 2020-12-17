using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WorkaroundUtilities
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);

            Thread.CurrentThread.Name = "main";

            //for dependency injection, logging etc.
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    //interface is useful to have testing classes implementing the interface
                    //and not writing to databases etc. 
                    services.AddTransient<IGreetingService, GreetingService>();
                    services.AddTransient<IWorkaroundPublisherService, WorkaroundPublisherService>();
                })
                .UseSerilog()
                .Build();

            //setup logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())              
                .CreateLogger();

            try
            {

                var greetingsService = ActivatorUtilities.CreateInstance<GreetingService>(host.Services);
                greetingsService.Run();

                var workaroundService = ActivatorUtilities.CreateInstance<WorkaroundPublisherService>(host.Services);
                workaroundService.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "application failed due to unresolved exception");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }
    }   
}
