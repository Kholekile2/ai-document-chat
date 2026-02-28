using AiDocChat.Api.DTOs;
using AiDocChat.Api.Services;

// Minimal API endpoints for document operations.
// Each endpoint is responsible for: reading the request, calling the
// service, and returning the appropriate HTTP response.

namespace AiDocChat.Api.Endpoints
{
    public static class DocumentEndpoints
    {
        public static void MapDocumentEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/documents");

            group.MapGet("/", async (HttpContext context, DocumentService documentService) =>
            {
                var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                var documents = await documentService.GetUserDocumentsAsync(userId);
                return Results.Ok(documents);
            });

            group.MapPost("/upload", async (
                HttpContext context,
                DocumentService documentService,
                PdfService pdfService,
                ChunkService chunkService) =>
            {
                var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                var file = context.Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Results.BadRequest(new { error = "No file provided" });

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "Only PDF files are accepted" });

                const long maxFileSize = 50 * 1024 * 1024;
                if (file.Length > maxFileSize)
                    return Results.BadRequest(new { error = "File size must be under 50MB" });

                var documentId = Guid.NewGuid();
                var storagePath = $"{userId}/{documentId}/{file.FileName}";

                // Save PDF to a temp file so PdfPig can read it
                var tempPath = Path.Combine(Path.GetTempPath(), $"{documentId}.pdf");
                using (var stream = File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                // Save metadata to Supabase first
                var document = await documentService.SaveDocumentMetadataAsync(
                    userId, file.FileName, file.Length, storagePath
                );

                // Extract text and chunk it — runs after metadata is saved
                // so the document appears in the UI immediately as "uploaded"
                try
                {
                    var extractedText = pdfService.ExtractText(tempPath);
                    var chunks = pdfService.ChunkText(extractedText);
                    await chunkService.SaveChunksAsync(document.Id, chunks);

                    // Update status to "ready" once processing is complete
                    await documentService.UpdateStatusAsync(document.Id, "ready");
                    document.Status = "ready";
                }
                catch
                {
                    await documentService.UpdateStatusAsync(document.Id, "failed");
                    document.Status = "failed";
                }
                finally
                {
                    // Always clean up the temp file regardless of success or failure
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }

                return Results.Ok(document);
            });

            app.MapDelete("/api/documents/{documentId}", async (
            HttpContext context,
            DocumentService documentService,
            string documentId) =>
            {
                var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                if (!Guid.TryParse(documentId, out var docGuid))
                    return Results.BadRequest(new { error = "Invalid document ID" });

                await documentService.DeleteDocumentAsync(docGuid);
                return Results.NoContent();
            });
        }
    }
}
