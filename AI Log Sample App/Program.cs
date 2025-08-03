using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using AILogger;

public class Program
{
    static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfiguration configuration = builder.Build();
        var chatGPTAPIKey = configuration["Logging:AILogger:ChatGPTAPIKey"];
        var applicationId = configuration["Logging:AILogger:ApplicationId"];
        using var loggerProvider = new AILoggerProvider(chatGPTAPIKey, applicationId ?? "Default");
        var logger = loggerProvider.CreateLogger("SampleCategory");
        try
        {
            dieFromNotImplemented();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}\r\nStack Trace:{ex.StackTrace}\r\nInnerException{ex.InnerException}");
        }
        try
        {
            var foo = dieFromBadMath(10, logger);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}\r\nStack Trace:{ex.StackTrace}\r\nInnerException{ex.InnerException}");
        }
        

        Console.WriteLine("Getting recommendations from AI Logger...");
        Console.WriteLine(loggerProvider.GetRecommendation());

    }
    private static int dieFromBadMath(int v, ILogger logger)
    {
        try
        {
            logger.LogWarning($"{v} / {(v - 1)} = {v/(v-1)}");
            return dieFromBadMath(v - 1, logger);
        }
        catch (StackOverflowException ex)
        {
            logger.LogError($"Stack overflow at depth {v}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error at depth {v}: {ex.Message}");
            throw;
        }
    }

    private static void dieFromNotImplemented()
    {
        throw new NotImplementedException();
    }
}   
