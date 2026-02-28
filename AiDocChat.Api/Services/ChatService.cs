using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi.Writers;


namespace AiDocChat.Api.Services
{
    public class ChatService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public ChatService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = config["OpenAI:ApiKey"]!;
        }

        // IAsyncEnumerable allows us to yield tokens one at a time as they
        // arrive from OpenAI, rather than waiting for the full response.
        // The frontend receives each token immediately as it's generated.
        public async IAsyncEnumerable<string> StreamChatAsync(
            string question,
            List<string> contextChunks,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Build the context string from the relevant chunks.
            // We number each chunk so the model can reference them clearly.
            var context = string.Join("\n\n", contextChunks.Select((chunk, i) =>
                $"[Excerpt {i + 1}]\n{chunk}"));

            // The system prompt instructs the model how to behave.
            // Being explicit about only using the provided context prevents
            // the model from making things up (hallucinating).
            var systemPrompt = """
            You are a helpful assistant that answers questions about documents.
            Answer the user's question using ONLY the provided excerpts from the document.
            If the answer cannot be found in the excerpts, say so clearly.
            Be concise and cite which excerpt supports your answer when relevant.
            """;

            var userPrompt = $"""
            Document excerpts:
            {context}

            Question: {question}
            """;

            var requestBody = new
            {
                model = "gpt-4o-mini",
                stream = true,   // tells OpenAI to stream tokens as they're generated
                messages = new[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
                max_tokens = 1000,
                temperature = 0.3  // lower temperature = more factual, less creative
            };

            // HttpCompletionOption.ResponseHeadersRead is critical for streaming —
            // it means we start reading the response body immediately rather than
            // waiting for the entire response to download first.
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"OpenAI chat failed: {error}");
            }

            // Read the response stream line by line.
            // OpenAI's streaming format sends Server-Sent Events (SSE):
            // each line looks like: data: {"choices":[{"delta":{"content":"Hello"}}]}
            // The final line is: data: [DONE]
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];

                if (data == "[DONE]") break;

                JsonDocument json;
                try
                {
                    json = JsonDocument.Parse(data);
                }
                catch
                {
                    continue;
                }

                // Navigate the SSE JSON structure to extract the token text.
                // Each event contains: choices[0].delta.content
                var content = json.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (content.TryGetProperty("content", out var tokenElement))
                {
                    var token = tokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                        yield return token;
                }
            }
        }
    }
}
