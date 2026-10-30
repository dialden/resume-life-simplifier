using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class RLSContext(DbContextOptions<RLSContext> options) : DbContext(options)
{
    public DbSet<PostingEntity> Postings => Set<PostingEntity>();

    public DbSet<JobSitePublicationEntity> JobSitePublications => Set<JobSitePublicationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity(entity => entity.ToTable("InboxState", "messaging"));
        modelBuilder.AddOutboxMessageEntity(entity => entity.ToTable("OutboxMessage", "messaging"));
        modelBuilder.AddOutboxStateEntity(entity => entity.ToTable("OutboxState", "messaging"));

        var posting = modelBuilder.Entity<PostingEntity>();
        posting.ToTable("postings");
        posting.HasKey(entity => entity.Id);
        posting.Property(entity => entity.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();
        posting.Property(entity => entity.Title)
            .HasColumnName("title")
            .IsRequired();
        posting.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        var publication = modelBuilder.Entity<JobSitePublicationEntity>();
        publication.ToTable("job_site_publications");
        publication.HasKey(entity => new { entity.PostingId, entity.JobSiteId });
        publication.Property(entity => entity.PostingId).HasColumnName("posting_id");
        publication.Property(entity => entity.JobSiteId).HasColumnName("job_site_id");
        publication.Property(entity => entity.Status).HasColumnName("status").HasConversion<string>();
        publication.HasOne<PostingEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.PostingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
