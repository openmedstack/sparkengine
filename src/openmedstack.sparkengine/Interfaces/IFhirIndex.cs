﻿/*
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Rest;

public interface IFhirIndex
{
    Task Clean();

    Task<SearchResults> Search(string resource, SearchParams searchCommand, CancellationToken cancellationToken);

    Task<Key?> FindSingle(string resource, SearchParams searchCommand, CancellationToken cancellationToken);

    Task<SearchResults> GetReverseIncludes(
        IReadOnlyList<IKey> keys,
        IReadOnlyList<string> revIncludes,
        CancellationToken cancellationToken = default);

    Task<SearchResults> GetIncludes(
        IReadOnlyList<IKey> keys,
        IReadOnlyList<string> includes,
        CancellationToken cancellationToken);
}
