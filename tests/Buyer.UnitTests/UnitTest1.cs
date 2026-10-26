namespace Buyer.UnitTests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void DomainAssemblyDoesNotReferenceApplicationProjects()
    {
        var references = typeof(Buyer.Domain.AssemblyMarker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => reference.Name?.StartsWith("Buyer.") == true);
    }
}
