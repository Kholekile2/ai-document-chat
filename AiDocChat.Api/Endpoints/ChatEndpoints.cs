namespace AiDocChat.Api.Endpoints;

using AiDocChat.Api.DTOs;
using AiDocChat.Api.Services;
using System.Text;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        // GET /api/chat/conversations?documentId=...
        // Returns all conversations for a user + document
        app.MapGet("/api/chat/conversations", async (
            HttpContext context,
            ConversationService conversationService,
            string documentId) =>
        {
            var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (!Guid.TryParse(documentId, out var docGuid))
                return Results.BadRequest(new { error = "Invalid document ID" });

            var conversations = await conversationService.GetConversationsAsync(userId, docGuid);
            return Results.Ok(conversations);
        });

        // GET /api/chat/conversations/{conversationId}/messages
        // Returns all messages in a conversation
        app.MapGet("/api/chat/conversations/{conversationId}/messages", async (
            HttpContext context,
            ConversationService conversationService,
            string conversationId) =>
        {
            var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (!Guid.TryParse(conversationId, out var convGuid))
                return Results.BadRequest(new { error = "Invalid conversation ID" });

            var messages = await conversationService.GetMessagesAsync(convGuid);
            return Results.Ok(messages);
        });

        // POST /api/chat — sends a message and streams the response
        app.MapPost("/api/chat", async (
            HttpContext context,
            SearchService searchService,
            ChatService chatService,
            ConversationService conversationService) =>
        {
            var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            ChatRequestDto? request;
            try
            {
                request = await context.Request.ReadFromJsonAsync<ChatRequestDto>();
            }
            catch
            {
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return Results.BadRequest(new { error = "Question cannot be empty" });

            // Create a new conversation if one isn't provided,
            // using the question as the conversation title
            Guid conversationId;
            if (request.ConversationId == Guid.Empty)
            {
                var conversation = await conversationService.CreateConversationAsync(
                    userId, request.DocumentId, request.Question);
                conversationId = conversation.Id;
            }
            else
            {
                conversationId = request.ConversationId;
            }

            // Save the user's message immediately before streaming starts
            await conversationService.SaveMessageAsync(conversationId, "user", request.Question);

            var relevantChunks = await searchService.FindRelevantChunksAsync(
                request.Question, request.DocumentId, topK: 5);

            if (relevantChunks.Count == 0)
                return Results.BadRequest(new { error = "No relevant content found in document" });

            var chunkContents = relevantChunks.Select(c => c.Content).ToList();

            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            // Send the conversation ID first so the frontend can track it
            var convIdBytes = Encoding.UTF8.GetBytes($"data: __CONV_ID__{conversationId}\n\n");
            await context.Response.Body.WriteAsync(convIdBytes);
            await context.Response.Body.FlushAsync();

            // Stream the response and accumulate the full answer
            var fullResponse = new StringBuilder();

            await foreach (var token in chatService.StreamChatAsync(
                request.Question, chunkContents, context.RequestAborted))
            {
                fullResponse.Append(token);
                var sseMessage = $"data: {token}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseMessage);
                await context.Response.Body.WriteAsync(bytes);
                await context.Response.Body.FlushAsync();
            }

            // Save the complete assistant response and update conversation timestamp
            await conversationService.SaveMessageAsync(conversationId, "assistant", fullResponse.ToString());
            await conversationService.UpdateConversationTimestampAsync(conversationId);

            var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
            await context.Response.Body.WriteAsync(doneBytes);
            await context.Response.Body.FlushAsync();

            return Results.Empty;
        });
    }
}