using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SKProcess;
using System.Diagnostics.Contracts;

#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0003, SKEXP0011, SKEXP0020, SKEXP0050, SKEXP0052, SKEXP0055, SKEXP0011, SKEXP0010, SKEXP0070

namespace DocUploader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("*** DocLoader ***");

            AppSettings setx = new();

            var kernelBuilder = Kernel.CreateBuilder();

            switch (setx.embeddingModelType.ToLower())
            {
                case "azopenai":
                    Console.WriteLine($"Using Azure OpenAI {setx.azopenaiEmbeddingDeploymentname}");
                    kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(setx.azopenaiEmbeddingDeploymentname, setx.azopwnaiEndpoint, setx.azopwnaiApikey);
                    break;
                case "ollama":
                    // docker run -d -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama
                    Console.WriteLine($"Using Ollama ");
                    //string ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "";
                    kernelBuilder.AddOllamaTextEmbeddingGeneration(setx.ollamaEmbeddingModelId, new Uri(setx.ollamaEndpoint));
                    break;
                case "bertonnx":
                    Console.WriteLine("Using Bert embedding");  // download from "git clone https://huggingface.co/TaylorAI/bge-micro-v2"
                    kernelBuilder.AddBertOnnxTextEmbeddingGeneration(setx.bertEmbeddingModelPath, setx.bertEmbeddingVocabPath);
                    break;
                case "huggingface":
                    Console.WriteLine("Using Hugging Face embedding");
                    kernelBuilder.AddHuggingFaceTextEmbeddingGeneration(new Uri(setx.hfEndPoint), setx.hfApikey);
                    break;
                default:
                    Console.WriteLine("Unsupported embedding model");
                    Environment.Exit(-1);
                    return;
            }

            switch (setx.vectorStoreType.ToLower())
            {
                case "memory":
                    Console.WriteLine("Using In-Memory");
                    kernelBuilder.AddInMemoryVectorStore();
                    break;
                case "azaisearch":
                    Console.WriteLine("Using Azure AI Search");
                    kernelBuilder.AddAzureAISearchVectorStore(new Uri(setx.azaisearchEndpoint), new AzureKeyCredential(setx.azaisearchApikey));
                    break;
                case "redis":
                    // docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
                    Console.WriteLine("Using Redis");
                    kernelBuilder.AddRedisVectorStore("localhost:6379");
                    break;
                case "cosmosdb-nosql":
                    Console.WriteLine("Using cosmosdb-nosql");
                    kernelBuilder.AddAzureCosmosDBNoSQLVectorStore(setx.cosmosdbConnectionString, setx.cosmosdbDatabaseName);
                    break;
                default:
                    Console.WriteLine("Unsupported vector store type");
                    Environment.Exit(-1);
                    return;
            }

            kernelBuilder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
            kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
            kernelBuilder.Services.AddSingleton<IConfiguration>(setx.configuration);
            kernelBuilder.Services.AddScoped<DocIntelligence>();
            kernelBuilder.Services.AddScoped<DataUploader>();
            kernelBuilder.Services.AddScoped<DocProcessor>();
            //kernelBuilder.Services.AddScoped<DocReaderPdf>();
            //kernelBuilder.Services.AddScoped<DocReaderWord>();

            var kernel = kernelBuilder.Build();

            await kernel.Services.GetRequiredService<DocProcessor>().ProcessAndUploadDocumentAsync(setx.dirPath + setx.docName);


        }

    }
}
