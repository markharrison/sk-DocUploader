using Microsoft.Extensions.Configuration;
using System.IO;
using UglyToad.PdfPig;

namespace DocUploader
{
    internal class DocReaderPdf
    {
        IConfiguration _config;
        int _chunkMaxWords = 200;
        int _chunkOverlapWords = 25;
        bool _traceOn = false;

        public DocReaderPdf(IConfiguration config)
        {
            _config = config;
            _chunkMaxWords = int.TryParse(config["Chunk:MaxWords"], out int maxWords) ? maxWords : 200;
            _chunkOverlapWords = int.TryParse(config["Chunk:OverlapWords"], out int overlapWords) ? overlapWords : 25;
            _traceOn = bool.TryParse(config["TraceOn"], out bool traceOn) && traceOn;
        }

        public async Task<List<TextChunk>> ProcessPdfDocumentAsync(string docpath)
        {
            await Task.Run(() => { });

            var TextChunks = new List<TextChunk>();
            int cntr = 1;
            string documentUriKey = Utils.GenerateKeyFromUrl(docpath);

            FileStream? stream = null;

            try
            {
                stream = new FileStream(docpath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            using (stream)
            {
                PdfDocument pdfDocument = PdfDocument.Open(stream);
                foreach (var page in pdfDocument.GetPages())
                {
                    var text = page.Text;

                    var chunks = Utils.SplitToChunks(text, _chunkMaxWords, _chunkOverlapWords);

                    foreach (var chunk in chunks)
                    {
                        if (!string.IsNullOrWhiteSpace(chunk))
                        {
                            if (_traceOn)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Document chunk {cntr}:");
                                Console.WriteLine(chunk);
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            TextChunks.Add(new TextChunk
                            {
                                Key = documentUriKey + (cntr).ToString("D4"),
                                DocumentUri = docpath,
                                ChunkId = $"{page.Number}-{cntr++}",
                                Text = chunk
                            });
                        }
                    }
                }
            }

            return TextChunks;
        }

    }
}