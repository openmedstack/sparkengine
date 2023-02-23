namespace OpenMedStack.Linq2Fhir.Tests;

using System.Net;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

internal class TestMessageHandler : HttpMessageHandler
{
    public string? RequestedPathAndQuery { get; private set; }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedPathAndQuery = request.RequestUri!.PathAndQuery;
        var serializer = new FhirJsonSerializer();
        var value = new Bundle { Type = Bundle.BundleType.Collection, Total = 0, Id = "1" };
        var json = await serializer.SerializeToStringAsync(value).ConfigureAwait(false);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(json, Encoding.UTF8, "application/fhir+json"),
            //JsonContent.Create(value, new MediaTypeHeaderValue("application/json+fhir")),
            Headers = { }
        };
        return responseMessage;
    }

    public class FhirResponse
    {
        public Key? Key { get; }
        public Resource? Resource { get; }
        public HttpStatusCode StatusCode { get; }

        public FhirResponse(HttpStatusCode code, Key? key = null, Resource? resource = null)
        {
            StatusCode = code;
            Key = key;
            Resource = resource;
        }

        public bool IsValid
        {
            get
            {
                var code = (int)StatusCode;
                return code <= 300;
            }
        }

        public bool HasBody => Resource != null;

        public override string ToString()
        {
            var details = Resource != null ? $"({Resource.TypeName})" : null;
            var location = Key?.ToString();
            return $"{(int)StatusCode}: {StatusCode} {details} ({location})";
        }
    }

    public class Key
    {
        public Key()
        {
        }

        public Key(string? @base, string? type, string? resourceId, string? versionId)
        {
            Base = @base?.TrimEnd('/');
            TypeName = type;
            ResourceId = resourceId;
            VersionId = versionId;
        }

        public string? Base { get; set; }
        public string? TypeName { get; set; }
        public string? ResourceId { get; set; }
        public string? VersionId { get; set; }

        public static Key Create(string type) => new(null, type, null, null);

        public static Key Create(string? type, string resourceId) => new(null, type, resourceId, null);

        public static Key Create(string type, string? resourceId, string? versionId) =>
            new(null, type, resourceId, versionId);

        public override bool Equals(object? obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Key)obj;
            return ToUriString(this) == ToUriString(other);
        }

        public static string ToUriString(Key? key)
        {
            if (key == null)
            {
                return "";
            }
            var segments = GetSegments(key);
            return string.Join("/", segments);
        }

        public static IEnumerable<string> GetSegments(Key key)
        {
            if (key.Base != null)
            {
                yield return key.Base;
            }

            if (key.TypeName != null)
            {
                yield return key.TypeName;
            }

            if (key.ResourceId != null)
            {
                yield return key.ResourceId;
            }

            if (key.VersionId != null)
            {
                yield return "_history";
                yield return key.VersionId;
            }
        }


        public override int GetHashCode() => ToUriString(this).GetHashCode();

        public static Key ParseOperationPath(string path)
        {
            var key = new Key();
            path = path.Trim('/');
            var indexOfQueryString = path.IndexOf('?');
            if (indexOfQueryString >= 0)
            {
                path = path[..indexOfQueryString];
            }

            var segments = path.Split('/');
            if (segments.Length >= 1)
            {
                key.TypeName = segments[0];
            }

            if (segments.Length >= 2)
            {
                key.ResourceId = segments[1];
            }

            if (segments is [_, _, "_history", _])
            {
                key.VersionId = segments[3];
            }

            return key;
        }

        public override string ToString() => ToUriString(this);
    }
}