namespace AiDocChat.Api
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Unique identifier for the document
        public string UserId { get; set; } = string.Empty; // Identifier for the user who owns the document
        public string FileName { get; set; } = string.Empty; // Name of the uploaded file
        public long FileSize { get; set; } // Size of the file in bytes
        public string StoragePath { get; set; } = string.Empty; // Path in Supabase Storage
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp of when the document was uploaded
        public string Status { get; set; } = "uploaded"; // Status of the document (e.g., "active", "archived")
    }
}