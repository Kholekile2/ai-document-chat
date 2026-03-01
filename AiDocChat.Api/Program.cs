using AiDocChat.Api.Endpoints;
using AiDocChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ChunkService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddControllers();

// AddHttpClient registers IHttpClientFactory for dependency injection.
// Always use IHttpClientFactory instead of creating HttpClient directly —
// it manages connection pooling and avoids socket exhaustion.
builder.Services.AddHttpClient();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Allow frontend to talk to the backend (CORS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://ai-document-chat-sigma.vercel.app"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Type");
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// NOTE: UseHttpsRedirection is intentionally omitted in Development —
// the frontend calls http://localhost:5014 directly and a redirect to
// HTTPS causes "Failed to fetch" / CORS preflight failures in the browser.

// Health check endpoint — useful to confirm the API is running
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.MapDocumentEndpoints();
app.MapChatEndpoints();
app.Run();