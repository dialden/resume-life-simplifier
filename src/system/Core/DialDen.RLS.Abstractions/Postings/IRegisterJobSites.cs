namespace DialDen.RLS.Abstractions.Postings;

public interface IRegisterJobSites
{
    /// <summary>
    /// Makes the job sites this process can publish on available for selection.
    /// The previously registered job sites it cannot publish on become unavailable.
    /// </summary>
    Task Register(CancellationToken cancellationToken);
}
