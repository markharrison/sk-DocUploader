using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;

namespace DocUploader
{
    internal class DocIntelligence
    {
        string _docintelliEndpoint;
        string _docintelliApikey;
        int _chunkMaxWords = 200;
        int _chunkOverlapWords = 25;
        bool _traceOn = false;

        public DocIntelligence(IConfiguration config)
        {
            _docintelliEndpoint = config["DocIntelli:Endpoint"] ?? "";
            _docintelliApikey = config["DocIntelli:ApiKey"] ?? "";
            _chunkMaxWords = int.TryParse(config["Chunk:MaxWords"], out int maxWords) ? maxWords : 200;
            _chunkOverlapWords = int.TryParse(config["Chunk:OverlapWords"], out int overlapWords) ? overlapWords : 25;
            _traceOn = bool.TryParse(config["TraceOn"], out bool traceOn) && traceOn;
        }

        public async Task<List<TextChunk>> ProcessDocumentAsync(string docpath)
        {
            int cntr = 1;
            string documentUriKey = Utils.GenerateKeyFromUrl(docpath);

            Console.WriteLine("Using Document Intelligence");

            var content = new AnalyzeDocumentContent();
            var TextChunks = new List<TextChunk>();

            var client = new DocumentIntelligenceClient(new Uri(_docintelliEndpoint), new AzureKeyCredential(_docintelliApikey));

            if (Utils.IsUrl(docpath))
            {
                content.UrlSource = new Uri(docpath); ;
            }
            else
            {
                try
                {
                    using FileStream stream = new FileStream(docpath, FileMode.Open, FileAccess.Read);
                    byte[] fileBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }
                    content.Base64Source = BinaryData.FromBytes(fileBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(-1 );
                }

            }


            Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
            AnalyzeResult result = operation.Value;

            foreach (DocumentPage page in result.Pages)
            {
                string pageText = "";

                for (int i = 0; i < result.Paragraphs.Count; i++)
                {
                    DocumentParagraph paragraph = result.Paragraphs[i];

                    pageText += paragraph.Content + " ";

                    if (_traceOn)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Paragraph {i}:");
                        Console.WriteLine($"  Content: {paragraph.Content} " + ((paragraph.Role != null) ? "  Role: {paragraph.Role}" : ""));
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                }

                var chunks = Utils.SplitToChunks(pageText, _chunkMaxWords,_chunkOverlapWords);

                foreach (var chunk in chunks)
                {
                    if (!string.IsNullOrWhiteSpace(chunk))
                    {
                        if (_traceOn)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Document chunk {cntr}:");
                            Console.WriteLine(chunk );
                            Console.ForegroundColor = ConsoleColor.White;
                        }

                        TextChunks.Add(new TextChunk
                        {
                            Key = documentUriKey + (cntr).ToString("D4"),
                            DocumentUri = docpath,
                            ChunkId = $"{page.PageNumber}-{cntr++}",
                            Text = chunk
                        });
                    }
                }

            }

            return TextChunks;

        }

    }
}
