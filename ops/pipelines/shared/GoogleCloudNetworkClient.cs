using System.Text.Json;

namespace DialDen.RLS.Pipelines;

public sealed class GoogleCloudNetworkClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public const string Network = "rls-serverless";
    public const string Subnet = "rls-serverless";
    private const string SubnetRange = "10.214.0.0/26";
    private static readonly string[] PrivateGoogleAddresses =
        ["199.36.153.8", "199.36.153.9", "199.36.153.10", "199.36.153.11"];

    public async Task EnsureNetwork()
    {
        using var networks = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["compute", "networks", "list", $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (!networks.RootElement.EnumerateArray().Any(item => item.GetProperty("name").GetString() == Network))
        {
            await processRunner.Run("gcloud",
                ["compute", "networks", "create", Network, $"--project={config.ProjectId}",
                    "--subnet-mode=custom", "--quiet"]);
        }
        using var network = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["compute", "networks", "describe", Network, $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (network.RootElement.GetProperty("autoCreateSubnetworks").GetBoolean())
            throw new InvalidOperationException($"Network '{Network}' must be a custom subnet network.");

        using var subnets = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["compute", "networks", "subnets", "list", $"--project={config.ProjectId}",
                $"--regions={config.Region}", "--format=json", "--quiet"], captureOutput: true));
        if (!subnets.RootElement.EnumerateArray().Any(item => item.GetProperty("name").GetString() == Subnet))
        {
            await processRunner.Run("gcloud",
                ["compute", "networks", "subnets", "create", Subnet, $"--project={config.ProjectId}",
                    $"--region={config.Region}", $"--network={Network}", $"--range={SubnetRange}",
                    "--enable-private-ip-google-access", "--quiet"]);
        }
        using var subnet = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["compute", "networks", "subnets", "describe", Subnet, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true));
        var subnetDetails = subnet.RootElement;
        if (subnetDetails.GetProperty("ipCidrRange").GetString() != SubnetRange ||
            subnetDetails.GetProperty("privateIpGoogleAccess").GetBoolean() != true ||
            subnetDetails.GetProperty("network").GetString() != network.RootElement.GetProperty("selfLink").GetString())
        {
            throw new InvalidOperationException(
                $"Subnet '{Subnet}' must belong to '{Network}', use {SubnetRange}, and enable Private Google Access.");
        }
        PipelineLog.Write($"Private Cloud Run network verified: {Network}/{Subnet} in {config.Region}.");
    }

    public async Task EnsureOllamaDns(string zoneName, string serviceUrl)
    {
        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps || !address.Host.EndsWith(".run.app", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Ollama service must have an HTTPS run.app URL.");
        }
        var dnsName = address.Host + ".";
        using var zones = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["dns", "managed-zones", "list", $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (!zones.RootElement.EnumerateArray().Any(item => item.GetProperty("name").GetString() == zoneName))
        {
            await processRunner.Run("gcloud",
                ["dns", "managed-zones", "create", zoneName, $"--project={config.ProjectId}",
                    $"--dns-name={dnsName}", "--visibility=private", $"--networks={Network}",
                    "--description=Private Ollama Cloud Run endpoint", "--quiet"]);
        }
        using var zone = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["dns", "managed-zones", "describe", zoneName, $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        var zoneDetails = zone.RootElement;
        var networkUrl = $"projects/{config.ProjectId}/global/networks/{Network}";
        if (zoneDetails.GetProperty("dnsName").GetString() != dnsName ||
            zoneDetails.GetProperty("visibility").GetString() != "private" ||
            !zoneDetails.GetProperty("privateVisibilityConfig").GetProperty("networks").EnumerateArray()
                .Any(item => item.GetProperty("networkUrl").GetString()?.EndsWith(networkUrl, StringComparison.Ordinal) == true))
        {
            throw new InvalidOperationException($"DNS zone '{zoneName}' does not match the private Ollama endpoint and network.");
        }

        using var records = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["dns", "record-sets", "list", $"--zone={zoneName}", $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        var existing = records.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("name").GetString() == dnsName &&
                item.GetProperty("type").GetString() == "A").ToArray();
        if (existing.Length == 0)
        {
            await processRunner.Run("gcloud",
                ["dns", "record-sets", "create", dnsName, $"--zone={zoneName}", $"--project={config.ProjectId}",
                    "--type=A", "--ttl=60", $"--rrdatas={string.Join(",", PrivateGoogleAddresses)}", "--quiet"]);
        }
        else if (existing.Length != 1 ||
            !existing[0].GetProperty("rrdatas").EnumerateArray().Select(item => item.GetString())
                .Order(StringComparer.Ordinal).SequenceEqual(PrivateGoogleAddresses.Order(StringComparer.Ordinal)))
        {
            throw new InvalidOperationException($"DNS record '{dnsName}' does not point to Private Google Access.");
        }
        PipelineLog.Write($"Private DNS configured for {address.Host}.");
    }
}
