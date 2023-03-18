namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Interfaces;
using Marten;
using Microsoft.Extensions.Logging;

public class MartenResourcePersistence : IResourcePersistence
{
    private readonly Func<IDocumentSession> _sessionFunc;
    private readonly ILogger<MartenResourcePersistence> _logger;

    public MartenResourcePersistence(Func<IDocumentSession> sessionFunc, ILogger<MartenResourcePersistence> logger)
    {
        _sessionFunc = sessionFunc;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> Store(Resource resource, CancellationToken cancellationToken)
    {
        try
        {
            var session = _sessionFunc();
            await using var _ = session.ConfigureAwait(false);
            session.Store(resource);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Resource?> Get(IKey key, CancellationToken cancellationToken)
    {
        try
        {
            var session = _sessionFunc();
            await using var _ = session.ConfigureAwait(false);
            var resource = await session.LoadAsync<Resource>(key.ResourceId!, cancellationToken).ConfigureAwait(false);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
            return null;
        }
    }
}