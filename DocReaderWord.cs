using System.Text;
using System.Xml;

using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;

namespace DocUploader
{
    internal class DocReaderWord
    {
        IConfiguration _config;
        int _chunkMaxWords = 200;
        int _chunkOverlapWords = 25;
        bool _traceOn = false;

        public DocReaderWord(IConfiguration config)
        {
            _config = config;
            _chunkMaxWords = int.TryParse(config["Chunk:MaxWords"], out int maxWords) ? maxWords : 200;
            _chunkOverlapWords = int.TryParse(config["Chunk:OverlapWords"], out int overlapWords) ? overlapWords : 25;
            _traceOn = bool.TryParse(config["TraceOn"], out bool traceOn) && traceOn;
        }

        public async Task<List<TextChunk>> ProcessWordDocumentAsync(string docpath)
        {
            string documentUriKey = Utils.GenerateKeyFromUrl(docpath);
            var TextChunks = new List<TextChunk>();
            int cntr = 1;

            void ProcessText(string txt)
            {
                var chunks = Utils.SplitToChunks(txt, _chunkMaxWords, _chunkOverlapWords);

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
                            ChunkId = $"{cntr++}",
                            Text = chunk
                        });
                    }
                }
            }

            await Task.Run(() => { });

            FileStream? stream = null;

            try
            {
                stream = new FileStream(docpath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            if (stream == null)
            {
                return TextChunks;
            }

            using (stream)
            {
                using WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false);
                if (wordDoc.MainDocumentPart == null)
                {
                    return TextChunks;
                }

                // Create an XmlDocument to hold the document contents and load the document contents into the XmlDocument.
                XmlDocument xmlDoc = new XmlDocument();
                XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                nsManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                nsManager.AddNamespace("w14", "http://schemas.microsoft.com/office/word/2010/wordml");

                xmlDoc.Load(wordDoc.MainDocumentPart.GetStream());

                // Select all paragraphs in the document and break if none found.
                XmlNodeList? paragraphs = xmlDoc.SelectNodes("//w:p", nsManager);
                if (paragraphs == null)
                {
                    return TextChunks;
                }

                string docText = string.Empty;

                foreach (XmlNode paragraph in paragraphs)
                {
                    // Select all text nodes in the paragraph and continue if none found.
                    XmlNodeList? texts = paragraph.SelectNodes(".//w:t", nsManager);
                    if (texts == null)
                    {
                        continue;
                    }

                    foreach (XmlNode text in texts)
                    {
                        if (!string.IsNullOrWhiteSpace(text.InnerText))
                        {
                            docText += text.InnerText;
                        }
                    }

                    if (docText.Length > 10000)
                    {
                        ProcessText(docText);
                        docText = docText.Substring(docText.Length - 100);
                    }
                }

                ProcessText(docText);

            }

            return TextChunks;
        }
    }
}