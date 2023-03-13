﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Threading.Tasks;
using Core;
using Interfaces;
using Microsoft.Extensions.Logging;
using Service;

public class LogListener : IServiceListener
{
    private readonly ILogger _logger;

    public LogListener(ILogger logger) => _logger = logger;

    /// <inheritdoc />
    public Task Inform(Uri location, Entry interaction)
    {
        _logger.LogDebug(
            $"{interaction.When} - {location} -> {interaction.Key}, {interaction.Method}, Is Delete: {interaction.IsDelete}, Is Present: {interaction.IsPresent}");

        return Task.CompletedTask;
    }
}