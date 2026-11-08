namespace DialDen.RLS.Abstractions.Applications;

public sealed class InvalidApplicationException(string field) : Exception
{
    public string Field { get; } = field;
}
