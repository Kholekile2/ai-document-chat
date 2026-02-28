// DTOs define the shape of data coming IN and going OUT of the API.
// We never expose our database models (Document.cs) directly to the API —
// DTOs give us control over exactly what the client sends and receives.

namespace AiDocChat.Api.DTOs
{
    // What the API returns after a successful document upload. We don't want to return the StoragePath
    public class DocumentResponseDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ProcessingResultDto
    {
        public Guid DocumentId { get; set; }
        public int ChunksCreated { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ChatRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public Guid DocumentId { get; set; }
        public Guid ConversationId { get; set; }
    }

    public class RelevantChunkDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public double Similarity { get; set; }
    }

    public class ConversationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
