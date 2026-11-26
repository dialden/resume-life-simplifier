using DialDen.RLS.Tests.Core.Clients;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.Configuration;
using WireMock.Client;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Owns one scenario's application hosts, database clone, and stub mappings.</summary>
public sealed class TestEnvironment : IAsyncDisposable
{
    private readonly IDatabaseCleanup _databaseCleanup;
    private readonly string _databaseName;
    private readonly IHttpHost _apiHost;
    private readonly IHttpHost? _uiHost;
    private readonly IWorkerHost _workerHost;
    private RLSConfiguration? _configuration;
    private bool _disposed;

    internal ScenarioDiagnostics Diagnostics { get; } = new();

    public RLSConfiguration Configuration =>
        _configuration ?? throw new InvalidOperationException("The API host has not been started.");

    public WireMockMappings? JobSiteMappings { get; }
    public WireMockMappings? ResumeAnalysisMappings { get; }
    public IReadOnlyList<string> JobSiteIds { get; }

    public TestEnvironment(string databaseName, string connectionString, ExternalSystems externalSystems,
        Channel channel, IConfiguration workerConfiguration,
        (Uri Url, IWireMockAdminApi Admin)? jobSitesStub,
        (Uri Url, IWireMockAdminApi Admin)? resumeAnalysisStub,
        IDatabaseCleanup databaseCleanup)
    {
        _databaseCleanup = databaseCleanup;
        _databaseName = databaseName;
        var configurationPrefix = externalSystems switch
        {
            ExternalSystems.Stubbed => "STUBBED_EXT_SYSTEMS_",
            ExternalSystems.Real => "REAL_EXT_SYSTEMS_",
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
            if (jobSitesStub is not { } jobSites || resumeAnalysisStub is not { } resumeAnalysis)
                throw new InvalidOperationException("Configure both WireMock servers for stubbed scenarios.");

            var pathPrefix = $"/harness/{databaseName}";
            var stubPath = pathPrefix.TrimStart('/') + "/";
            JobSiteMappings = new(jobSites.Admin, pathPrefix, jobSites.Url);
            ResumeAnalysisMappings = new(resumeAnalysis.Admin, pathPrefix, resumeAnalysis.Url);
            workerSettings["JobSites:Type"] = "stubbed";
            workerSettings["Ollama:Endpoint"] = new Uri(resumeAnalysis.Url, stubPath).ToString();
            workerSettings["Ollama:IdentityTokenAudience"] = "https://test-resume-analysis.run.app";
            workerSettings["Ollama:AllowInsecureAuthenticatedEndpoint"] = "true";
            workerSettings["Ollama:IdentityMetadataEndpoint"] = new Uri(resumeAnalysis.Url,
                stubPath + "computeMetadata/v1/instance/service-accounts/default/identity").ToString();
            foreach (var site in sites)
                workerSettings[$"JobSites:Sites:{site.Key}:Url"] =
                    new Uri(jobSites.Url, stubPath).ToString();
        }
        _workerHost = new InProcessWorkerHost(configurationPrefix, workerSettings, Diagnostics);
        _apiHost = new InProcessAPIHttpHost(configurationPrefix, settings, Diagnostics);
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
        Diagnostics.Record("Worker", "Starting host");
        _workerHost.Start();
        Diagnostics.Record("Worker", "Host started");
        Diagnostics.Record("API", "Starting host");
        _apiHost.Start();
        Diagnostics.Record("API", "Host started");
        _configuration = new(_apiHost.Url);
        if (_uiHost is not null)
        {
            Diagnostics.Record("UI", "Starting host");
            _uiHost.Start();
            Diagnostics.Record("UI", "Host started");
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
            () => _databaseCleanup.DropDatabaseAsync(_databaseName),
            () => JobSiteMappings?.DisposeAsync() ?? ValueTask.CompletedTask,
            () => ResumeAnalysisMappings?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}