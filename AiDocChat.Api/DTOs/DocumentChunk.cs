// Represents a single chunk of text extracted from a document.
// Each chunk gets its own embedding vector in Phase 3.

namespace AiDocChat.Api.DTOs
{
    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique identifier for the chunk
        public Guid DocumentId { get; set; } // Reference to the original document
        public string Content { get; set; } = string.Empty; // The text content of the chunk
        public string ContentIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp for when the chunk was created
    }
}
