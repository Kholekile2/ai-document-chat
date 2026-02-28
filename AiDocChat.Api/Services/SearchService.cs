using System.Text;
using System.Text.Json;
using AiDocChat.Api.DTOs;
using System.Text.Json.Serialization;


namespace AiDocChat.Api.Services
{
    public class SearchService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly EmbeddingService _embeddingService;

        public SearchService(IHttpClientFactory httpClientFactory, IConfiguration config, EmbeddingService embeddingService)
        {
            _http = httpClientFactory.CreateClient();
            _embeddingService = embeddingService;
            _supabaseUrl = config["Supabase:Url"]!;
            _supabaseKey = config["Supabase:ServiceKey"]!;
        }

        private void SetSupabaseHeaders()
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
        }

        // Finds the most semantically similar chunks to the user's question.
        // This is the core of RAG — finding the right context before answering.
        public async Task<List<RelevantChunkDto>> FindRelevantChunksAsync(
    string question,
    Guid documentId,
    int topK = 5)
        {
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

            var payload = new
            {
                query_embedding = questionEmbedding, //$"[{string.Join(",", questionEmbedding.Select(f => f.ToString("G", System.Globalization.CultureInfo.InvariantCulture)))}]",
                match_document_id = documentId.ToString(),
                match_count = topK
            };

            // Use HttpRequestMessage so we can set headers per-request
            // rather than on the shared HttpClient instance
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_supabaseUrl}/rest/v1/rpc/match_document_chunks");

            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Similarity search failed: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var chunks = JsonSerializer.Deserialize<List<SupabaseChunkResult>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return chunks!.Select(c => new RelevantChunkDto
            {
                Id = c.Id,
                Content = c.Content,
                ChunkIndex = c.ChunkIndex,
                Similarity = c.Similarity
            }).ToList();
        }

        // This class matches the shape of the data returned by our Supabase RPC function.
        private class SupabaseChunkResult
        {
            [JsonPropertyName("id")]
            public Guid Id { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            [JsonPropertyName("chunk_index")]
            public int ChunkIndex { get; set; }

            [JsonPropertyName("similarity")]
            public double Similarity { get; set; }
        }

    }
}
