#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:project ../pipelines/shared/DialDen.RLS.Pipelines.csproj

using DialDen.RLS.Pipelines;

// Run only for initial token setup or rotation. Each successful run adds a secret
// version, even for an unchanged token; running the release script does not.

if (args.Length != 0 || !Console.IsInputRedirected)
{
    throw new ArgumentException("Supply the GitHub token through stdin, with no arguments. See README.md.");
}

var token = (await Console.In.ReadToEndAsync()).Trim();
if (token.Length == 0 || token.Any(char.IsWhiteSpace))
{
    throw new ArgumentException("Supply one non-empty GitHub token through stdin.");
}

var root = PipelineWorkspace.ResolveRoot();
var config = PipelineWorkspace.ReadConfig(root).GoogleCloud
    ?? throw new InvalidOperationException($"{PipelineWorkspace.ConfigFile} has no googleCloud section.");
config.Validate();
var artifacts = Path.Combine(root, ".artifacts", "registry-token",
    $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
Directory.CreateDirectory(artifacts);
var googleCloud = new GoogleCloudClient(new ProcessRunner(root, artifacts), config);

await googleCloud.VerifyAuthentication();
await googleCloud.AddSecretVersion("ghcr-read-token", token);
PipelineLog.Write("Added a new ghcr-read-token version. Existing versions were left unchanged.");
