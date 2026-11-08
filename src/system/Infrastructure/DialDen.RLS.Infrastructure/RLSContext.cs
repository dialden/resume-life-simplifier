using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.JobApplications;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure;

internal sealed class RLSContext(DbContextOptions<RLSContext> options) : DbContext(options)
{
    public DbSet<Posting> Postings => Set<Posting>();

    public DbSet<JobSitePublication> JobSitePublications => Set<JobSitePublication>();

    public DbSet<JobSite> JobSites => Set<JobSite>();
    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity(entity => entity.ToTable("InboxState", "messaging"));
        modelBuilder.AddOutboxMessageEntity(entity => entity.ToTable("OutboxMessage", "messaging"));
        modelBuilder.AddOutboxStateEntity(entity => entity.ToTable("OutboxState", "messaging"));

        var posting = modelBuilder.Entity<Posting>();
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
        posting.Property(entity => entity.Description).HasColumnName("description").IsRequired();

        var publication = modelBuilder.Entity<JobSitePublication>();
        publication.ToTable("job_site_publications");
        publication.HasKey(entity => new { entity.PostingId, entity.JobSiteId });
        publication.Property(entity => entity.PostingId).HasColumnName("posting_id");
        publication.Property(entity => entity.JobSiteId).HasColumnName("job_site_id");
        publication.Property(entity => entity.Status).HasColumnName("status").HasConversion<string>();
        publication.Ignore(entity => entity.IsPending);
        publication.HasOne<Posting>()
            .WithMany()
            .HasForeignKey(entity => entity.PostingId)
            .OnDelete(DeleteBehavior.Cascade);

        var jobSite = modelBuilder.Entity<JobSite>();
        jobSite.ToTable("job_sites");
        jobSite.HasKey(entity => entity.Id);
        jobSite.Property(entity => entity.Id).HasColumnName("id");
        jobSite.Property(entity => entity.Name).HasColumnName("name").IsRequired();
        jobSite.Property<bool>("IsAvailable").HasColumnName("is_available");

        var application = modelBuilder.Entity<Application>();
        application.ToTable("applications");
        application.HasKey(entity => entity.Id);
        application.Property(entity => entity.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        application.Property(entity => entity.PostingId).HasColumnName("posting_id");
        application.Property(entity => entity.Name).HasColumnName("name").IsRequired();
        application.Property(entity => entity.Email).HasColumnName("email").IsRequired();
        application.Property(entity => entity.ResumeFileName).HasColumnName("resume_file_name").IsRequired();
        application.Property(entity => entity.Resume).HasColumnName("resume").IsRequired();
        application.Property(entity => entity.Source).HasColumnName("source").IsRequired();
        application.Property(entity => entity.ReferralJobSiteId).HasColumnName("referral_job_site_id");
        application.HasOne<Posting>().WithMany().HasForeignKey(entity => entity.PostingId);
    }
}
