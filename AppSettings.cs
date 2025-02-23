using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace SKProcess
{
    public class AppSettings
    {
        public string azopwnaiApikey { get; set; }
        public string azopwnaiEndpoint { get; set; }
        public string azopenaiCCDeploymentname { get; set; }
        public string azopenaiEmbeddingDeploymentname { get; set; }
        public bool traceOn { get; set; }
        public string bingApikey { get; set; }
        public string bingEndpoint { get; set; }
        public string embeddingModelType { get; set; }
        public string vectorStoreType { get; set; }
        public string collectionName { get; set; }
        public string ollamaEndpoint { get; set; }
        public string ollamaEmbeddingModelId { get; set; }
        public string bertEmbeddingModelPath { get; set; }
        public string bertEmbeddingVocabPath { get; set; }
        public string hfEndPoint { get; set; }
        public string hfApikey { get; set; }
        public string azaisearchEndpoint { get; set; }
        public string azaisearchApikey { get; set; }
        public string cosmosdbConnectionString { get; set; }
        public string cosmosdbDatabaseName { get; set; }
        public string docName { get; set; }
        public string dirPath { get; set; }

        public ConfigurationManager configuration;

        public AppSettings()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";
            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();

            configuration = hostBuilder.Configuration;

            azopwnaiApikey = configuration["AzOpenAI:ApiKey"] ?? "";
            azopwnaiEndpoint = configuration["AzOpenAI:Endpoint"] ?? "";
            azopenaiCCDeploymentname = configuration["AzOpenAI:ChatCompletionDeploymentName"] ?? "";
            azopenaiEmbeddingDeploymentname = configuration["AzOpenAI:EmbeddingDeploymentName"] ?? "";
            ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "";
            ollamaEmbeddingModelId = configuration["Ollama:EmbeddingModelId"] ?? "";
            bertEmbeddingModelPath = configuration["BertOnnx:EmbeddingModelPath"] ?? "";
            bertEmbeddingVocabPath = configuration["BertOnnx:EmbeddingVocabPath"] ?? "";
            hfEndPoint = configuration["HuggingFace:EndPoint"] ?? "";
            hfApikey = configuration["HuggingFace:Apikey"] ?? "";
            azaisearchEndpoint = configuration["AzAISearch:Endpoint"] ?? "";
            azaisearchApikey = configuration["AzAISearch:ApiKey"] ?? "";
            cosmosdbConnectionString = configuration["CosmosDB:ConnectionString"] ?? "";
            cosmosdbDatabaseName = configuration["CosmosDB:DatabaseName"] ?? "";
            traceOn = bool.TryParse(configuration["TraceOn"], out bool tOn) && tOn;
            bingApikey = configuration["Bing:ApiKey"] ?? "";
            bingEndpoint = configuration["Bing:Endpoint"] ?? "";
            embeddingModelType = configuration["EmbeddingModelType"] ?? "";
            collectionName = configuration["CollectionName"] ?? "";
            vectorStoreType = configuration["VectorStoreType"] ?? "";
            docName = configuration["Document:Name"] ?? "";
            dirPath = configuration["Document:DirPath"] ?? "";
            if (!dirPath.EndsWith("/")) dirPath += "/";

        }

    }
}

