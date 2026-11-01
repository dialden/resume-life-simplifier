using DialDen.RLS.Abstractions.Postings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.Worker;

/// <summary>Makes the job sites this worker can publish on available before it starts consuming.</summary>
internal sealed class JobSitesRegistration(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IRegisterJobSites>().Register(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
