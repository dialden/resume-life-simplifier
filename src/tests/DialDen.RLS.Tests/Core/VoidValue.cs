namespace DialDen.RLS.Tests.Core;

public class VoidValue
{
    private VoidValue() { }

    public static VoidValue Empty { get; } = new VoidValue();
}
