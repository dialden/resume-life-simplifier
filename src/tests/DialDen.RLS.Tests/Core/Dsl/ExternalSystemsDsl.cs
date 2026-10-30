using DialDen.RLS.Tests.Core.Dsl.JobSites;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Dsl;

/// <summary>Entry point for the DSLs of external systems. Add one DSL per external system here.</summary>
public class ExternalSystemsDsl
{
    private readonly IServiceProvider _serviceProvider;

    public ExternalSystemsDsl(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Resolved lazily: only environments with stubbed external systems can control them
    public JobSitesDsl JobSites => _serviceProvider.GetService<JobSitesDsl>()
        ?? throw new InvalidOperationException("Controlling job sites requires the stubbed external systems environment.");
}
