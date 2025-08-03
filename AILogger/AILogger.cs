using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace AILogger
{
    //gpt-4.1-mini

    internal class AILoggerOptionsSetup : ConfigureFromConfigurationOptions<AILoggerOptions>
    {
        public AILoggerOptionsSetup(ILoggerProviderConfiguration<AILoggerProvider>
                                      providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
    public class AILoggerOptions
    {
        #region Properties
        public ChatClient? _chatClient;
        public LogLevel LogLevel;
        public string? GPT_API_Key;
        public string ApplicationId;
        #endregion
        public AILoggerOptions(string? gptKey, string? appId, LogLevel logLevel) 
        {
            GPT_API_Key = gptKey;
            ApplicationId = appId ?? "";
            LogLevel = logLevel;
            _chatClient = new(model: "gpt-4.1-mini", apiKey: GPT_API_Key);
        }

    }
    [ProviderAlias("DBLogger")]
    public class AILoggerProvider : ILoggerProvider
    {
        IDisposable? SettingsChangeToken;
        public AILoggerOptions _aiLoggerOptions { get; set; }
        public AILogger? _aILogger { get; set; } 
        public AILoggerProvider(string? chatGPTAPIKey, string applicationId = "") 
        {
            _aiLoggerOptions = new AILoggerOptions(chatGPTAPIKey,  applicationId, Microsoft.Extensions.Logging.LogLevel.Information );
        }
        public AILoggerProvider(AILoggerOptions aiLoggerOptions) 
        {
            _aiLoggerOptions = aiLoggerOptions;
        }
        public AILoggerProvider(IOptionsMonitor<AILoggerOptions> aiLoggerOptions) : this(aiLoggerOptions.CurrentValue) 
        {
            SettingsChangeToken = aiLoggerOptions.OnChange(settings => { this._aiLoggerOptions = settings; });
        }
        public ILogger CreateLogger(string categoryName)
        {
            if(null == _aILogger)
            {
                _aILogger = new AILogger(this, categoryName);
            }
            return _aILogger;
        }
        public string GetRecommendation()
        {
           return _aILogger?.GetRecommendation() ?? "Insufficient information to make a recommendation.";
        }

        public void Dispose()
        {
            //TODO
            
        }
    }

    public class AILogger : ILogger
    {
        #region Properties
        private readonly AILoggerProvider _provider;
        private readonly string _category;
        private readonly string _hostName;
        private List<ChatMessage> _logBuffer = new List<ChatMessage>();
        #endregion

        public AILogger(AILoggerProvider provider, string categoryName) 
        {
            _category = categoryName;
            _provider = provider;
            _hostName = Environment.MachineName;
            _logBuffer.Add(new SystemChatMessage($"You are an expert software engineer and log analyzer. You will analyze logs from the application with Application Id {_provider._aiLoggerOptions.ApplicationId} running on host {_hostName}. Provide recommendations based on the logs provided. If you see errors or warnings, provide suggestions to fix them. If you see patterns in the logs, provide insights. Be concise and to the point."));
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None
            && Convert.ToInt32(logLevel) >= Convert.ToInt32( _provider._aiLoggerOptions.LogLevel);
        }
        public string GetRecommendation()
        {
           
            ChatCompletion chatCompletion = _provider._aiLoggerOptions._chatClient!.CompleteChat(_logBuffer);
            return chatCompletion.Content[0].Text;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
                     
            try
            {
                if(_logBuffer.Count > 200)
                {
                    _logBuffer.RemoveRange(1, _logBuffer.Count - 10);
                }
                _logBuffer.Add(new UserChatMessage($"[{DateTimeOffset.Now}] [{logLevel}] [{eventId.Id}] [{_category}] {formatter(state, exception)}"));
            }
            catch {
                // It is painful to do a catch and eat the exception, but we don't want to cause the application to fail because of logging.
                // Best we can do is hope the other logs are working.
            }

        }
    }
}
