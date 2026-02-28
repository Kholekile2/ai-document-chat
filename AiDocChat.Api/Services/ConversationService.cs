using AiDocChat.Api.DTOs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace AiDocChat.Api.Services
{
    public class ConversationService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public ConversationService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient();
            _supabaseUrl = config["Supabase:Url"]!;
            _supabaseKey = config["Supabase:ServiceKey"]!;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
            return request;
        }

        public async Task<ConversationDto> CreateConversationAsync(
            string userId,
            Guid documentId,
            string title)
        {
            var request = CreateRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/conversations");
            request.Headers.Add("Prefer", "return=representation");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    user_id = userId,
                    document_id = documentId.ToString(),
                    title = title.Length > 100 ? title[..100] : title
                }),
                Encoding.UTF8, "application/json"
            );

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<SupabaseConversation>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var conv = results!.First();
            return new ConversationDto
            {
                Id = conv.Id,
                Title = conv.Title,
                CreatedAt = conv.CreatedAt,
                UpdatedAt = conv.UpdatedAt
            };
        }

        public async Task<List<ConversationDto>> GetConversationsAsync(
            string userId,
            Guid documentId)
        {
            var request = CreateRequest(HttpMethod.Get,
                $"{_supabaseUrl}/rest/v1/conversations?user_id=eq.{userId}&document_id=eq.{documentId}&order=updated_at.desc");
            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var conversations = JsonSerializer.Deserialize<List<SupabaseConversation>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return conversations!.Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId)
        {
            var request = CreateRequest(HttpMethod.Get,
                $"{_supabaseUrl}/rest/v1/messages?conversation_id=eq.{conversationId}&order=created_at.asc");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var messages = JsonSerializer.Deserialize<List<SupabaseMessage>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return messages!.Select(m => new MessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task SaveMessageAsync(Guid conversationId, string role, string content)
        {
            var request = CreateRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/messages");
            request.Headers.Add("Prefer", "return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    conversation_id = conversationId.ToString(),
                    role,
                    content
                }),
                Encoding.UTF8, "application/json"
            );

            await _http.SendAsync(request);
        }

        public async Task UpdateConversationTimestampAsync(Guid conversationId)
        {
            var request = CreateRequest(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/conversations?id=eq.{conversationId}");
            request.Headers.Add("Prefer", "return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { updated_at = DateTime.UtcNow }),
                Encoding.UTF8, "application/json"
            );

            await _http.SendAsync(request);
        }

        private class SupabaseConversation
        {
            [JsonPropertyName("id")] public Guid Id { get; set; }
            [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
            [JsonPropertyName("document_id")] public Guid DocumentId { get; set; }
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
            [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
        }

        private class SupabaseMessage
        {
            [JsonPropertyName("id")] public Guid Id { get; set; }
            [JsonPropertyName("conversation_id")] public Guid ConversationId { get; set; }
            [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
            [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
            [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        }
    }
}
