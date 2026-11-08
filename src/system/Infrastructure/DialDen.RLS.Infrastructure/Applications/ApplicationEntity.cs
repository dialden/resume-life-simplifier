namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class ApplicationEntity
{
    public int Id { get; set; }
    public int PostingId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string ResumeFileName { get; set; }
    public required byte[] Resume { get; set; }
    public required string Source { get; set; }
    public string? ReferralJobSiteId { get; set; }
}
