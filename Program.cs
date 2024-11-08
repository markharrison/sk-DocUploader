using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Diagnostics.Contracts;

#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0003, SKEXP0011, SKEXP0020, SKEXP0050, SKEXP0052, SKEXP0055, SKEXP0011, SKEXP0010, SKEXP0070

namespace DocUploader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("*** DocLoader ***");

            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";
            var hostBuilder = Host.CreateApplicationBuilder(args);
            hostBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();

            var configuration = hostBuilder.Configuration;

            string docName = configuration["Document:Name"] ?? "";
            string dirPath = configuration["Document:DirPath"] ?? "";
            if (!dirPath.EndsWith("/")) dirPath += "/";
            string vectorStoreType = configuration["VectorStoreType"] ?? "";
            string embeddingModelType = configuration["EmbeddingModelType"] ?? "";

            var kernelBuilder = Kernel.CreateBuilder();

            switch (embeddingModelType.ToLower())
            {
                case "azopenai":
                    string azopwnaiApikey = configuration["AzOpenAI:ApiKey"] ?? "";
                    string azopwnaiEndpoint = configuration["AzOpenAI:Endpoint"] ?? "";
                    string azopenaiCCDeploymentname = configuration["AzOpenAI:ChatCompletionDeploymentName"] ?? "";
                    string azopenaiEmbeddingDeploymentname = configuration["AzOpenAI:EmbeddingDeploymentName"] ?? "";
                    Console.WriteLine($"Using Azure OpenAI {azopenaiEmbeddingDeploymentname}");
                    kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(azopenaiEmbeddingDeploymentname, azopwnaiEndpoint, azopwnaiApikey);
                    break;
                case "ollama":
                    // docker run -d -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama
                    Console.WriteLine($"Using Ollama ");
                    string ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "";
                    string ollamaEmbeddingModelId = configuration["Ollama:EmbeddingModelId"] ?? "";
                    kernelBuilder.AddOllamaTextEmbeddingGeneration(ollamaEmbeddingModelId, new Uri(ollamaEndpoint));
                    break;
                case "bertonnx":
                    Console.WriteLine("Using Bert embedding");  // download from "git clone https://huggingface.co/TaylorAI/bge-micro-v2"
                    string bertEmbeddingModelPath = configuration["BertOnnx:EmbeddingModelPath"]!;
                    string BertEmbeddingVocabPath = configuration["BertOnnx:EmbeddingVocabPath"]!;
                    kernelBuilder.AddBertOnnxTextEmbeddingGeneration(bertEmbeddingModelPath, BertEmbeddingVocabPath);
                    break;
                case "huggingface":
                    Console.WriteLine("Using Hugging Face embedding");
                    string hfEndPoint = configuration["HuggingFace:EndPoint"]!;
                    string hfApikey = configuration["HuggingFace:Apikey"]!;
                    kernelBuilder.AddHuggingFaceTextEmbeddingGeneration(new Uri(hfEndPoint), hfApikey);
                    break;
                default:
                    Console.WriteLine("Unsupported embedding model");
                    Environment.Exit(-1);
                    return;
            }

            switch (vectorStoreType.ToLower())
            {
                case "memory":
                    Console.WriteLine("Using In-Memory");
                    kernelBuilder.AddInMemoryVectorStore();
                    break;
                case "azaisearch":
                    Console.WriteLine("Using Azure AI Search");
                    string azaisearchEndpoint = configuration["AzAISearch:Endpoint"] ?? "";
                    string azaisearchApikey = configuration["AzAISearch:ApiKey"] ?? "";
                    kernelBuilder.AddAzureAISearchVectorStore(new Uri(azaisearchEndpoint), new AzureKeyCredential(azaisearchApikey));
                    break;
                case "redis":
                    // docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
                    Console.WriteLine("Using Redis");
                    kernelBuilder.AddRedisVectorStore("localhost:6379");
                    break;
                case "cosmosdb-nosql":
                    Console.WriteLine("Using cosmosdb-nosql");
                    string cosmosdbConnectionString = configuration["CosmosDB:ConnectionString"] ?? "";
                    string cosmosdbDatabaseName = configuration["CosmosDB:DatabaseName"] ?? "";
                    kernelBuilder.AddAzureCosmosDBNoSQLVectorStore(cosmosdbConnectionString, cosmosdbDatabaseName);
                    break;
                default:
                    Console.WriteLine("Unsupported vector store type");
                    Environment.Exit(-1);
                    return;
            }

            kernelBuilder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
            kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
            kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);
            kernelBuilder.Services.AddScoped<DocIntelligence>();
            kernelBuilder.Services.AddScoped<DataUploader>();
            kernelBuilder.Services.AddScoped<DocProcessor>();
            //kernelBuilder.Services.AddScoped<DocReaderPdf>();
            //kernelBuilder.Services.AddScoped<DocReaderWord>();

            var kernel = kernelBuilder.Build();

            await kernel.Services.GetRequiredService<DocProcessor>().ProcessAndUploadDocumentAsync(dirPath + docName);


        }

    }
}
