using AiDocChat.Api.DTOs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Services contain the business logic. Endpoints stay thin and
// just handle HTTP concerns — the service does the actual work.

namespace AiDocChat.Api.Services
{
    public class DocumentService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        // We use HttpClient to call Supabase's REST API instead of connecting
        // directly to PostgreSQL. This works over HTTPS (port 443) which is
        // never blocked, unlike PostgreSQL's port 5432.
        public DocumentService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient();
            _supabaseUrl = config["Supabase:Url"]!;
            _supabaseKey = config["Supabase:ServiceKey"]!;
        }

        // Creates a fresh HttpRequestMessage with Supabase headers set per-request.
        // We never use DefaultRequestHeaders because it's shared across all requests
        // on the same HttpClient instance — setting headers there causes race conditions
        // and missing headers when multiple requests are in flight at the same time.
        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
            return request;
        }

        public async Task<DocumentResponseDto> SaveDocumentMetadataAsync(
            string userId,
            string fileName,
            long fileSize,
            string storagePath)
        {
            var request = CreateRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/documents");

            // "Prefer: return=representation" tells Supabase to return the inserted row
            // so we can get the generated ID and created_at timestamp back immediately.
            request.Headers.Add("Prefer", "return=representation");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    user_id = userId,
                    file_name = fileName,
                    file_size = fileSize,
                    storage_path = storagePath,
                    status = "uploaded"
                }),
                Encoding.UTF8, "application/json"
            );

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase insert failed: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Supabase returns an array even for single inserts
            var documents = JsonSerializer.Deserialize<List<SupabaseDocument>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var doc = documents!.First();

            return new DocumentResponseDto
            {
                Id = doc.Id,
                FileName = doc.FileName,
                FileSize = doc.FileSize,
                Status = doc.Status,
                CreatedAt = doc.CreatedAt
            };
        }

        public async Task<List<DocumentResponseDto>> GetUserDocumentsAsync(string userId)
        {
            // Supabase REST API supports filtering via query parameters.
            // eq. means "equals" — this filters rows where user_id = userId.
            var request = CreateRequest(HttpMethod.Get,
                $"{_supabaseUrl}/rest/v1/documents?user_id=eq.{userId}&order=created_at.desc");

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<DocumentResponseDto>();

            var json = await response.Content.ReadAsStringAsync();
            var documents = JsonSerializer.Deserialize<List<SupabaseDocument>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return documents!.Select(doc => new DocumentResponseDto
            {
                Id = doc.Id,
                FileName = doc.FileName,
                FileSize = doc.FileSize,
                Status = doc.Status,
                CreatedAt = doc.CreatedAt
            }).ToList();
        }

        public async Task UpdateStatusAsync(Guid documentId, string status)
        {
            // PATCH updates only the specified fields — we only change status.
            // The ?id=eq.{documentId} filters to the specific document row.
            var request = CreateRequest(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/documents?id=eq.{documentId}");

            request.Headers.Add("Prefer", "return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { status }),
                Encoding.UTF8, "application/json"
            );

            await _http.SendAsync(request);
        }

        public async Task DeleteDocumentAsync(Guid documentId)
        {
            // DELETE removes the document row. The ON DELETE CASCADE on document_chunks
            // and conversations means all related data is automatically deleted too.
            var request = CreateRequest(HttpMethod.Delete,
                $"{_supabaseUrl}/rest/v1/documents?id=eq.{documentId}");

            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Delete failed: {error}");
            }
        }

        // Internal class that matches the exact JSON shape Supabase returns
        private class SupabaseDocument
        {
            [JsonPropertyName("id")]
            public Guid Id { get; set; }

            [JsonPropertyName("user_id")]
            public string UserId { get; set; } = string.Empty;

            [JsonPropertyName("file_name")]
            public string FileName { get; set; } = string.Empty;

            [JsonPropertyName("file_size")]
            public long FileSize { get; set; }

            [JsonPropertyName("storage_path")]
            public string StoragePath { get; set; } = string.Empty;

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }
    }
}