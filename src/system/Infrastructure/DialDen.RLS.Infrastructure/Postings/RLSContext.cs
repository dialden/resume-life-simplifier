using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class RLSContext(DbContextOptions<RLSContext> options) : DbContext(options)
{
    public DbSet<PostingEntity> Postings => Set<PostingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
