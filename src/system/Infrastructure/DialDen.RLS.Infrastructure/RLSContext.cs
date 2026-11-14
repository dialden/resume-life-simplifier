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
        application.Property(entity => entity.ExtractionStatus).HasColumnName("extraction_status")
            .HasConversion<string>().IsConcurrencyToken();
        application.Ignore(entity => entity.ExtractedContacts);
        application.Ignore(entity => entity.ExtractedEducation);
        application.Property(entity => entity.ExtractedName).HasColumnName("extracted_name");
        application.Property(entity => entity.ExtractedEmail).HasColumnName("extracted_email");
        application.Property(entity => entity.ExtractedPhoneNumber).HasColumnName("extracted_phone_number");
        application.Property(entity => entity.ExtractedPostalAddress).HasColumnName("extracted_postal_address");
        application.Property(entity => entity.ExtractedLinkedInUrl).HasColumnName("extracted_linkedin_url");
        application.OwnsMany(entity => entity.EducationHistory, education =>
        {
            education.ToTable("application_education");
            education.WithOwner().HasForeignKey("application_id");
            education.Property<int>("id").HasColumnName("id").UseIdentityByDefaultColumn();
            education.HasKey("id");
            education.Property(entity => entity.Position).HasColumnName("position");
            education.Property(entity => entity.Institution).HasColumnName("institution");
            education.Property(entity => entity.Qualification).HasColumnName("qualification");
            education.Property(entity => entity.FieldOfStudy).HasColumnName("field_of_study");
            education.Property(entity => entity.StartDate).HasColumnName("start_date");
            education.Property(entity => entity.EndDate).HasColumnName("end_date");
        });
        application.Navigation(entity => entity.EducationHistory)
            .HasField("_educationHistory").UsePropertyAccessMode(PropertyAccessMode.Field);
        application.HasOne<Posting>().WithMany().HasForeignKey(entity => entity.PostingId);
    }
}
