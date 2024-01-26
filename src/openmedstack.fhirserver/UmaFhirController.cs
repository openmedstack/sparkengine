namespace OpenMedStack.FhirServer;

using System.Threading;
using System.Threading.Tasks;
using DotAuth.Uma.Web;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;
using OpenMedStack.SparkEngine.Web.Controllers;

[Route("uma")]
public class UmaFhirController : FhirController
{
    public UmaFhirController(IFhirService fhirService, IFhirModel model)
        : base(fhirService, model)
    {
    }

    /// <inheritdoc />
    [UmaFilter("{0}/{1}", ["type", "id"], resourceSetAccessScope: "read")]
    public override Task<ActionResult<FhirResponse>> Read(string type, string id, CancellationToken cancellationToken)
    {
        return base.Read(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [UmaFilter("{0}/{1}/_history/{2}", ["type", "id", "vid"], resourceSetAccessScope: "read")]
    public override Task<FhirResponse> VRead(string type, string id, string vid, CancellationToken cancellationToken)
    {
        return base.VRead(type, id, vid, cancellationToken);
    }

    // [UmaFilter("{0}", new[] { "type" }, allowedScope: "create")]
    [OwnResourceFilter]
    public override Task<FhirResponse?> Create(string type, Resource resource, CancellationToken cancellationToken)
    {
        return base.Create(type, resource, cancellationToken);
    }

    [UmaFilter("{0}/{1}", ["type", "id"], resourceSetAccessScope: "delete")]
    public override Task<FhirResponse> Delete(string type, string id, CancellationToken cancellationToken)
    {
        return base.Delete(type, id, cancellationToken);
    }

    [UmaFilter("{0}/{1}/$everything", ["type", "id"], resourceSetAccessScope: "read")]
    public override Task<FhirResponse> Everything(string type, string id, CancellationToken cancellationToken)
    {
        return base.Everything(type, id, cancellationToken);
    }

    /// <inheritdoc />
    [UmaFilter("{0}", ["type"], resourceSetAccessScope: "search")]
    public override Task<FhirResponse> Search(
        string type,
        CancellationToken cancellationToken)
    {
        return base.Search(type, cancellationToken);
    }

    [UmaFilter("transaction", new string[0], resourceSetAccessScope: "transaction")]
    public override Task<FhirResponse> Transaction(Bundle bundle, CancellationToken cancellationToken)
    {
        return base.Transaction(bundle, cancellationToken);
    }
}
