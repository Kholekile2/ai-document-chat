using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDocChat.Api.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public EmbeddingService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["OpenAI:ApiKey"]!;
        }

        // Creates a fresh HttpRequestMessage with OpenAI auth header per-request.
        // Using DefaultRequestHeaders on a shared HttpClient causes race conditions
        // when multiple requests are in flight — headers bleed between requests.
        private HttpRequestMessage CreateRequest(string url, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            return request;
        }

        // Generates an embedding vector for a single piece of text by calling OpenAI's API.
        // Returns a float array of 1536 numbers representing the semantic meaning of the text.
        // Texts with similar meanings produce mathematically similar arrays — this is what
        // enables semantic search in Phase 3.
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = CreateRequest("https://api.openai.com/v1/embeddings", new
            {
                input = text,
                model = "text-embedding-ada-002"
            });

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI embedding failed: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(json);
            return result!.Data[0].Embedding;
        }

        // Generates embeddings for multiple chunks sequentially.
        // We process one at a time to avoid hitting OpenAI's rate limits.
        // In production with a higher-tier account this could be parallelised.
        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingAsync(text);
                embeddings.Add(embedding);
            }
            return embeddings;
        }

        private class EmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<EmbeddingData> Data { get; set; } = new();
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}