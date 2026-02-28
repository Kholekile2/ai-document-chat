// PURPOSE: AppDbContext is the main class for database interaction in EF Core.
// It represents your database as C# objects — each DbSet property
// corresponds to a table in your PostgreSQL database.
// PATTERN: The DbContext pattern is how Entity Framework Core works.
// You define your tables as DbSet<T> properties, define your models
// as C# classes, then EF Core handles the SQL for you.
// WHEN TO USE IN OTHER PROJECTS: Any time you use EF Core, you need
// a DbContext. One DbContext per application is the standard approach.

using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace AiDocChat.Api
{

    public class AppDbContext : DbContext
    {
        // The constructor receives DbContextOptions which contains the
        // connection string and provider (PostgreSQL in our case).
        // This is passed in automatically by dependency injection
        // because we registered it in Program.cs with AddDbContext.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ─── DB SETS (TABLES) ─────────────────────────────────────────────────
        // Each DbSet<T> maps to a table in the database.
        // We'll add more tables here as we build Phase 2 and 3.
        // For now we define the Documents table which we'll need in Phase 2.

        // Represents the "documents" table — stores metadata about uploaded PDFs.
        // The actual PDF files go to Supabase Storage, but metadata (name, size,
        // upload date, which user owns it) lives here in PostgreSQL.
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // OnModelCreating is where you configure table names, relationships,
            // indexes, and constraints using the Fluent API.
            // WHEN TO USE IN OTHER PROJECTS: Use this method to customize how
            // EF Core maps your C# classes to database tables.

            // Configure the Documents table
            modelBuilder.Entity<Document>(entity =>
            {
                // Set the actual table name in PostgreSQL (lowercase, snake_case is convention)
                entity.ToTable("documents");

                // Define the primary key
                entity.HasKey(d => d.Id);

                // Configure each column
                entity.Property(d => d.Id).HasColumnName("id");
                entity.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(d => d.FileName).HasColumnName("file_name").IsRequired();
                entity.Property(d => d.FileSize).HasColumnName("file_size");
                entity.Property(d => d.StoragePath).HasColumnName("storage_path").IsRequired();
                entity.Property(d => d.CreatedAt).HasColumnName("created_at");
                entity.Property(d => d.Status).HasColumnName("status").IsRequired();
            });
        }
    }
}