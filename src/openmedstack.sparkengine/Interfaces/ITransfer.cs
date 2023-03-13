// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Service;

public interface ITransfer
{
    IAsyncEnumerable<Entry> Externalize(IAsyncEnumerable<Entry> interactions);
    IEnumerable<Entry> Externalize(IEnumerable<Entry> interactions);
    Entry Externalize(Entry interaction);
    IAsyncEnumerable<Entry> Internalize(
        IEnumerable<Entry> interactions,
        Mapper<string, IKey>? mapper,
        CancellationToken cancellationToken);
    Task Internalize(Entry entry, CancellationToken cancellationToken);
}