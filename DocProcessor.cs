using Microsoft.Extensions.Configuration;

namespace DocUploader
{
    internal class DocProcessor
    {
        private readonly DataUploader _dataUploader;
        private readonly DocIntelligence _docIntelli;
        //private readonly DocReaderPdf _docPDF;
        //private readonly DocReaderWord _docWord;

        public DocProcessor(DataUploader dataUploader, IConfiguration config, DocIntelligence docIntelli/*, DocReaderPdf docPdf, DocReaderWord docWord*/)
        {
            _dataUploader = dataUploader;
            _docIntelli = docIntelli;
            //_docPDF = docPdf;
            //_docWord = docWord;
        }

        public async Task ProcessAndUploadDocumentAsync(string docpath)
        {
            var textChunks = new List<TextChunk>();

            Console.WriteLine($"Processing {docpath}");

            switch (Utils.GetFileExtension(docpath))
            {
                //case "pdf":
                //    textChunks = await _docPDF.ProcessPdfDocumentAsync(docpath);
                //    break;
                //case "docx":
                //    textChunks = await _docWord.ProcessWordDocumentAsync(docpath);
                //    break;
                case "docx":
                case "pdf":
                    textChunks = await _docIntelli.ProcessDocumentAsync(docpath);
                    break;
                default:
                    Console.WriteLine("Unsupported file type.");
                    return;
            }

            Console.WriteLine($"Extracted {textChunks.Count} chunks.");
            if (textChunks.Count == 0)
            {
                Environment.Exit(-1);
            }

            await _dataUploader.GenEmbeddingsAndUploadChunksAsync( textChunks);
        }
    }
}
