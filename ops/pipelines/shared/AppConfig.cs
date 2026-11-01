namespace DialDen.RLS.Pipelines;

public sealed record AppConfig(
    string Solution, string ApiProject, string UiProject,
    string WorkerProject, string TestProject, string DockerDirectory);
