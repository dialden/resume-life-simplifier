namespace DialDen.RLS.Abstractions.Applications;

/// <summary>The submitted résumé and its original file name.</summary>
public sealed record ResumeResponse(string FileName, byte[] Content);
