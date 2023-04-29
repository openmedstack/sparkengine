using Hl7.Fhir.Rest;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

public class UmaFhirClient : FhirClient
{
    private class UmaHttpClient : HttpClient
    {
        private readonly HttpClient _innerClient;

        public UmaHttpClient(HttpClient innerClient)
        {
            _innerClient = innerClient;
        }

        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = _innerClient.Send(request, cancellationToken);
            return response;
        }

        public override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            foreach (var header in DefaultRequestHeaders)
            {
                if (request.Headers.All(x => x.Key != header.Key))
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            var response = await _innerClient.SendAsync(request, cancellationToken);
            return response;
        }
    }

    public UmaFhirClient(Uri endpoint, HttpClient httpClient, FhirClientSettings? settings = null) : base(endpoint,
        new UmaHttpClient(httpClient), settings)
    {
    }

    public UmaFhirClient(string endpoint, HttpClient httpClient, FhirClientSettings? settings = null) : base(endpoint,
        new UmaHttpClient(httpClient), settings)
    {
    }
}
