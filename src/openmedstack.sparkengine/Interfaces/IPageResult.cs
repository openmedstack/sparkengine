﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Interfaces;

using System.Collections.Generic;

public interface IPageResult<out T>
{
    long TotalRecords { get; }

    long TotalPages { get; }

    IAsyncEnumerable<T> IterateAllPages();
}