﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

using System;

namespace OpenMedStack.SparkEngine.Extensions;

using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Interfaces;

public static class GeneratorKeyExtensions
{
    public static async Task<Key> NextHistoryKey(
        this IGenerator generator,
        IKey key,
        CancellationToken cancellationToken)
    {
        var historyKey = key.Clone();
        historyKey.VersionId = await generator
            .NextVersionId(key.TypeName.AsMemory(), key.ResourceId.AsMemory(), key.VersionId.AsMemory(),
                cancellationToken)
            .ConfigureAwait(false);
        return historyKey;
    }

    public static async Task<Key> NextKey(
        this IGenerator generator,
        Resource resource,
        CancellationToken cancellationToken)
    {
        var resourceId = await generator.NextResourceId(resource, cancellationToken).ConfigureAwait(false);
        var key = resource.ExtractKey();
        var versionId = await generator
            .NextVersionId(key.TypeName.AsMemory(), resourceId.AsMemory(), key.VersionId.AsMemory(), cancellationToken)
            .ConfigureAwait(false);
        return Key.Create(key.TypeName!, resourceId, versionId);
    }
}
