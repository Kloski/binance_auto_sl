using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleFunctionTrigger;

public class Helper
{
    // generic type T is used to get the name of the class


    public static void InitContext(ILogger logger, string configValPrefix = "Values:")
    {
        logger.LogInformation("Base dir: {BaseDir}, current dir: {CurrentDir}", AppDomain.CurrentDomain.BaseDirectory, Environment.CurrentDirectory);
        logger.LogDebug("Base dir files: {Files}", string.Join(", ", Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)));
        logger.LogDebug("Current dir files: {Files}", string.Join(", ", Directory.GetFiles(Environment.CurrentDirectory)));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", true, true)
            .Build();

        logger.LogInformation("Config values: {ConfigVals}", String.Join(", ", configuration.AsEnumerable().ToList()));


        configuration.AsEnumerable()
            .Where(i => i.Key.StartsWith(configValPrefix))
            .ToList().ForEach(i => Environment.SetEnvironmentVariable(i.Key.Substring(configValPrefix.Length), i.Value));
    }

    private static ILogger<T> CreateServiceLogger<T>()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        // https://stackoverflow.com/questions/43424095/how-to-unit-test-with-ilogger-in-asp-net-core
        var factory = serviceProvider.GetService<ILoggerFactory>();
        return factory!.CreateLogger<T>();
    }

    public static ILogger<T> CreateConsoleLogger<T>()
    {
        // https://stackoverflow.com/questions/60688112/logging-in-console-application-net-core-with-di
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole();
        });
        return loggerFactory.CreateLogger<T>();
    }
}