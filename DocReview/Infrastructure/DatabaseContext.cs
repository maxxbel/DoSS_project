using DocReview.DataModels;
using Microsoft.EntityFrameworkCore;

namespace DocReview.Infrastructure;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ReviewRequest> ReviewRequests => Set<ReviewRequest>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(e => e.PasswordSalt).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<ReviewRequest>(entity =>
        {
            entity.HasKey(e => e.Id); 
            entity.Property(e => e.Title).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4096);
            entity.Property(e => e.DocumentReference).HasMaxLength(4096).IsRequired();
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedReviewer)
                .WithMany()
                .HasForeignKey(d => d.AssignedReviewerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id); 
            entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.MediaType).HasMaxLength(256);
            entity.HasOne(e => e.ReviewRequest)
                .WithMany(e => e.Versions)
                .HasForeignKey(e => e.ReviewRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).HasMaxLength(4096).IsRequired();
            entity.HasOne(e => e.ReviewRequest)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.ReviewRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
}