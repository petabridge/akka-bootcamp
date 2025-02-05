using Akka.Hosting;
using AkkaWordCounter2.App.Config;
using Microsoft.Extensions.Configuration;
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
        services.AddAkka("MyActorSystem", (builder, sp) => { });
    });

var host = hostBuilder.Build();

await host.RunAsync();