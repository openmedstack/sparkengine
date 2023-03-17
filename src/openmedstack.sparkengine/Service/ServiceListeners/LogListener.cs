// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.ServiceListeners;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;

public class LogListener : IServiceListener
{
    private readonly ILogger<LogListener> _logger;

    public LogListener(ILogger<LogListener> logger) => _logger = logger;

    /// <inheritdoc />
    public Task Inform(Uri location, Entry interaction)
    {
        _logger.LogDebug(
            "{when} - {location} -> {key}, {method}, Is Delete: {isDelete}, Is Present: {isPresent}",
            interaction.When,
            location,
            interaction.Key,
            interaction.Method,
            interaction.IsDelete,
            interaction.IsPresent);

        return Task.CompletedTask;
    }
}