using DialDen.RLS.DependencyInjection;
using DialDen.RLS.Infrastructure.DependencyInjection;
using DialDen.RLS.JobSites.Jobillico.DependencyInjection;
using DialDen.RLS.Worker.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.Worker.Root;

/// <summary>Composes the worker process. Shared by its entry point and the in-process acceptance tests.</summary>
public static class WorkerHost
{
    public const string EnvironmentVariablesPrefix = "RLS_WORKER_";

    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddEnvironmentVariables(prefix: EnvironmentVariablesPrefix)
            .AddCommandLine(args);

        builder.Services
            .AddRLSCore()
            .AddRLSInfrastructure(builder.Configuration, bus => bus.AddRLSConsumers())
            .AddRLSWorker()
            .AddJobillico();

        return builder;
    }
}
