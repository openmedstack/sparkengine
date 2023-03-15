﻿namespace OpenMedStack.FhirServer;

using System.Threading.Tasks;
using DotAuth.Uma;
using Npgsql;
using NpgsqlTypes;
using Weasel.Postgresql;

public class DbSourceMap : IResourceMap
{
    private readonly string _connectionString;

    public DbSourceMap(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<string?> GetResourceSetId(string resourceId)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await using var _ = connection.ConfigureAwait(false);
        await connection.OpenAsync().ConfigureAwait(false);
        var command = connection.CreateCommand();
        await using var __ = command.ConfigureAwait(false);
        command.CommandText = "SELECT resource_set_id FROM resource_map WHERE resource_id = @resourceId LIMIT 1";
        command.AddNamedParameter("resourceId", resourceId, NpgsqlDbType.Varchar);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        await connection.CloseAsync().ConfigureAwait(false);

        return result as string;
    }

    /// <inheritdoc />
    public async Task<string?> GetResourceId(string resourceSetId)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await using var _ = connection.ConfigureAwait(false);
        await connection.OpenAsync().ConfigureAwait(false);
        var command = connection.CreateCommand();
        await using var __ = command.ConfigureAwait(false);
        command.CommandText = "SELECT resource_id FROM resource_map WHERE resource_set_id = @resourceSetId LIMIT 1";
        command.AddNamedParameter("resourceSetId", resourceSetId, NpgsqlDbType.Varchar);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        await connection.CloseAsync().ConfigureAwait(false);

        return result as string;
    }
}