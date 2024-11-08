using Microsoft.Extensions.VectorData;

namespace DocUploader
{

#pragma warning disable SKEXP0001


    // Base class without the TextEmbedding property
    internal class TextChunk
    {
        [VectorStoreRecordKey]
        public required string Key { get; init; }

        [VectorStoreRecordData]
        public required string DocumentUri { get; init; }

        [VectorStoreRecordData]
        public required string ChunkId { get; init; }

        [VectorStoreRecordData(IsFullTextSearchable = true)]
        public required string Text { get; init; }

        [VectorStoreRecordVector]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }

    internal class TextChunk384 : TextChunk
    {
        [VectorStoreRecordVector(384)]
        public new ReadOnlyMemory<float> TextEmbedding { get; set; }
    }

    internal class TextChunk768 : TextChunk
    {
        [VectorStoreRecordVector(768)]
        public new ReadOnlyMemory<float> TextEmbedding { get; set; }
    }

    internal class TextChunk1536 : TextChunk
    {
        [VectorStoreRecordVector(1536 )]
        public new ReadOnlyMemory<float> TextEmbedding { get; set; }
    }

}
