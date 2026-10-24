namespace DialDen.RLS.Tests.Core.Dsl;

public class VoidVerification : ResponseVerification<VoidValue>
{
    public VoidVerification(VoidValue response, UseCaseContext context)
        : base(response, context)
    {
    }
}
