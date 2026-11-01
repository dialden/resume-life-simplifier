namespace DialDen.RLS.JobSites.Jobillico;

public sealed class JobillicoOptions
{
    public string Endpoint { get; set; } = "https://www.jobillico.com/api/push";

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public string RecruiterId { get; set; } = "";

    public string LocationId { get; set; } = "";

    public bool UseProduction { get; set; }
}
