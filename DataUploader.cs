using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using NetTopologySuite.Geometries;

#pragma warning disable SKEXP0001 

namespace DocUploader
{
    internal class DataUploader
    {
        private readonly IVectorStore _vectorStore;
        private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService;
        private readonly string _collectionName;
        bool _traceOn = false;
        string _embeddingModelType;

        public DataUploader(IConfiguration config, IVectorStore vectorStore, ITextEmbeddingGenerationService textEmbeddingGenerationService)
        {
            _vectorStore = vectorStore;
            _textEmbeddingGenerationService = textEmbeddingGenerationService;
            _traceOn = bool.TryParse(config["TraceOn"], out bool traceOn) && traceOn;
            _collectionName = (config["CollectionName"] ?? "markcoll").ToLower();
            _embeddingModelType = (config["EmbeddingModelType"] ?? "").ToLower();
        }


        public async Task CreateCollectionIfNotExistsAsync(int dimensions)
        {
            if (await _vectorStore.GetCollection<string, TextChunk>(_collectionName).CollectionExistsAsync() == false)
            {
                switch (dimensions)
                {
                    case 384:
                        await _vectorStore.GetCollection<string, TextChunk384>(_collectionName).CreateCollectionIfNotExistsAsync();
                        break;
                    case 768:
                        await _vectorStore.GetCollection<string, TextChunk768>(_collectionName).CreateCollectionIfNotExistsAsync();
                        break;
                    case 1536:
                    default:
                        await _vectorStore.GetCollection<string, TextChunk1536>(_collectionName).CreateCollectionIfNotExistsAsync();
                        break;
                }

                Console.WriteLine($"Created collection {_collectionName}");
            }
        }

        public async Task DeleteExistingChunksAsync(string documentUri)
        {
            string documentUriKey = Utils.GenerateKeyFromUrl(documentUri);
            var collection = _vectorStore.GetCollection<string, TextChunk>(_collectionName);
            int cntr = 1;
            while (true)
            {
                string key = documentUriKey + (cntr++).ToString("D4");
                var record = await collection.GetAsync(key);
                if (record == null) break;
                await collection.DeleteAsync(key);
                if (cntr == 2)
                {
                    Console.WriteLine($"Deleted old data");
                }
            }
        }

        public async Task GenEmbeddingsAndUploadChunksAsync(IEnumerable<TextChunk> TextChunks)
        {
            bool firstChunk = true;

            var collection = _vectorStore.GetCollection<string, TextChunk>(_collectionName);

            foreach (var textChunk in TextChunks)
            {
                textChunk.TextEmbedding = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(textChunk.Text);
                if (_traceOn)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Generated embedding for chunk: {textChunk.ChunkId}");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                if ( firstChunk)
                {
                    // we now know the dimensions length usued by the emdedding model 

                    await CreateCollectionIfNotExistsAsync(textChunk.TextEmbedding.Length);

                    await DeleteExistingChunksAsync(textChunk.DocumentUri);

                    firstChunk = false;
                }

                await collection.UpsertAsync(textChunk);
                if (_traceOn)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"   Inserted chunk: {textChunk.ChunkId}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            Console.WriteLine($"Uploaded document to collection {_collectionName}");

        }

    }
}
