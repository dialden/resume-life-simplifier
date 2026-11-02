namespace DialDen.RLS.Pipelines;

public sealed record PipelineConfig(AppConfig App, ImagesConfig Images, GoogleCloudConfig GoogleCloud);
