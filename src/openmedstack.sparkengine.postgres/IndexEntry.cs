﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Collections.Generic;

public class IndexEntry : IEquatable<IndexEntry>
{
    public IndexEntry(string id, string canonicalId, string resourceType, Dictionary<string, object[]> values)
    {
        Id = id;
        CanonicalId = canonicalId;
        ResourceType = resourceType;
        Values = values;
    }

    public string Id { get; }

    public string CanonicalId { get; }

    public string ResourceType { get; }

    public Dictionary<string, object[]> Values { get; }

    /// <inheritdoc />
    public bool Equals(IndexEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Id == other.Id && Equals(Values, other.Values);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as IndexEntry);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, ResourceType, Values);
}