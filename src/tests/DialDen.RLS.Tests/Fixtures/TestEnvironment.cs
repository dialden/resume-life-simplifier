using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.Configuration;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Owns one scenario's application hosts, database clone, and external-system namespace.</summary>
public sealed class TestEnvironment : IAsyncDisposable
{
    private readonly TestHarnessResources _resources;
    private readonly string _databaseName;
    private readonly IHttpHost _apiHost;
    private readonly IHttpHost? _uiHost;
    private readonly IWorkerHost _workerHost;
    private RLSConfiguration? _configuration;
    private bool _disposed;

    public RLSConfiguration Configuration =>
        _configuration ?? throw new InvalidOperationException("The API host has not been started.");

    public string ExternalSystemsPath { get; }
    public IReadOnlyList<string> JobSiteIds { get; }

    public TestEnvironment(TestHarnessResources resources, string databaseName, string connectionString,
        ExternalSystems externalSystems, Channel channel)
    {
        _resources = resources;
        _databaseName = databaseName;
        ExternalSystemsPath = $"/harness/{databaseName}";
        var (configurationPrefix, workerConfiguration) = externalSystems switch
        {
            ExternalSystems.Stubbed => ("STUBBED_EXT_SYSTEMS_", resources.WorkerConfiguration),
            ExternalSystems.Real => ("REAL_EXT_SYSTEMS_", resources.RealWorkerConfiguration),
            _ => throw new ArgumentOutOfRangeException(nameof(externalSystems), externalSystems, null)
        };
        var sites = externalSystems == ExternalSystems.Stubbed
            ? workerConfiguration.GetSection("JobSites:Sites").GetChildren().ToArray()
            : [];
        JobSiteIds = sites.Select(site => site["Id"]
            ?? throw new InvalidOperationException("Each stubbed job site requires an Id.")).ToArray();
        if (externalSystems == ExternalSystems.Stubbed && JobSiteIds.Count == 0)
            throw new InvalidOperationException("Configure stubbed worker job sites for the harness.");

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:RLS"] = connectionString,
            ["Messaging:JobSitePublicationQueue"] = "rls-job-site-publication",
            ["Messaging:ApplicationReceivedTopic"] = "rls-application-received",
            ["Messaging:ResumeExtractionQueue"] = "rls-resume-extraction"
        };
        var workerSettings = new Dictionary<string, string?>(settings);
        if (externalSystems == ExternalSystems.Stubbed)
        {
            workerSettings["JobSites:Type"] = "stubbed";
            workerSettings["Ollama:Endpoint"] =
                new Uri(resources.ResumeAnalysisUrl, ExternalSystemsPath.TrimStart('/') + "/").ToString();
            workerSettings["Ollama:IdentityTokenAudience"] = "https://test-resume-analysis.run.app";
            workerSettings["Ollama:AllowInsecureAuthenticatedEndpoint"] = "true";
            workerSettings["Ollama:IdentityMetadataEndpoint"] = new Uri(resources.ResumeAnalysisUrl,
                ExternalSystemsPath.TrimStart('/') + "/computeMetadata/v1/instance/service-accounts/default/identity").ToString();
            foreach (var site in sites)
                workerSettings[$"JobSites:Sites:{site.Key}:Url"] =
                    new Uri(resources.JobSitesUrl, ExternalSystemsPath.TrimStart('/') + "/").ToString();
        }
        _workerHost = new InProcessWorkerHost(configurationPrefix, workerSettings);
        _apiHost = new InProcessAPIHttpHost(configurationPrefix, settings);
        _uiHost = channel switch
        {
            Channel.API => null,
            Channel.UI => new InProcessUIHttpHost(() => _apiHost.Url),
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _workerHost.Start();
        _apiHost.Start();
        _configuration = new(_apiHost.Url);
        if (_uiHost is not null)
        {
            _uiHost.Start();
            _configuration.SetUIUrl(_uiHost.Url);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await TestResourceCleanup.RunAsync(
            () => { _uiHost?.Dispose(); return ValueTask.CompletedTask; },
            () => { _apiHost.Dispose(); return ValueTask.CompletedTask; },
            () => { _workerHost.Dispose(); return ValueTask.CompletedTask; },
            () => _resources.DropDatabaseAsync(_databaseName));
    }
}