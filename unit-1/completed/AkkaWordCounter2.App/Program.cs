using Akka.Hosting;
using AkkaWordCounter2.App.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder();


hostBuilder
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddWordCounterSettings();
        services.AddHttpClient(); // needed for IHttpClientFactory
        services.AddAkka("MyActorSystem", (builder, sp) =>
        {
            builder
                .ConfigureLoggers(logConfig =>
                {
                    logConfig.AddLoggerFactory();
                })
                .AddApplicationActors();
        });
    });

var host = hostBuilder.Build();

await host.RunAsync();