namespace OpenMedStack.Linq2Fhir.Tests;

using Hl7.Fhir.Model;

internal class TestType : Resource
{
    public string Subject { get; set; } = "";

    public int Number { get; set; }

    /// <inheritdoc />
    public override IDeepCopyable DeepCopy()
    {
        return this;
    }
}