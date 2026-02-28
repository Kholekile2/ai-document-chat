using System.Text;
using System.Text.Json;

namespace AiDocChat.Api.Services
{
    public class ChunkService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly EmbeddingService _embeddingService;

        public ChunkService(IHttpClientFactory httpClientFactory, IConfiguration config, EmbeddingService embeddingService)
        {
            _http = httpClientFactory.CreateClient();
            _supabaseUrl = config["Supabase:Url"]!;
            _supabaseKey = config["Supabase:ServiceKey"]!;
            _embeddingService = embeddingService;
        }

        // Creates a fresh HttpRequestMessage with Supabase headers set per-request.
        // Never use DefaultRequestHeaders on a shared HttpClient — headers set there
        // are shared across all concurrent requests, causing race conditions.
        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
            return request;
        }

        public async Task SaveChunksAsync(Guid documentId, List<string> chunks)
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);

            // embeddings[index] is a raw float array — when serialized via
            // SerializeToElement it becomes a proper JSON array [0.1, 0.2, ...]
            // NOT a string "[0.1, 0.2, ...]". This distinction is critical —
            // the Supabase RPC function's ::vector cast requires a real JSON array,
            // and the similarity search only works when save and search use the same format.
            var rows = chunks.Select((content, index) => new
            {
                id = Guid.NewGuid().ToString(),
                document_id = documentId.ToString(),
                content = content,
                chunk_index = index,
                embedding = embeddings[index]
            }).ToList();

            // SerializeToElement embeds rows as a proper JSON array inline —
            // not as a double-encoded string.
            var chunksJson = JsonSerializer.SerializeToElement(rows);
            var payload = JsonSerializer.Serialize(new { chunks = chunksJson });

            var request = CreateRequest(HttpMethod.Post,
                $"{_supabaseUrl}/rest/v1/rpc/insert_document_chunks");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to save chunks: {error}");
            }
        }
    }
}