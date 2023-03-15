// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service;

using System;
using System.Collections.Generic;
using System.Linq;

public class Mapper<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _mapping = new();

    public TValue? TryGet(TKey key)
    {
        return _mapping.TryGetValue(key, out var value) ? value : default;
    }

    public bool Exists(TKey key)
    {
        return _mapping.Any(item => item.Key.Equals(key));
    }

    public void Remap(TKey key, TValue value)
    {
        if (Exists(key))
        {
            _mapping[key] = value;
        }
        else
        {
            _mapping.Add(key, value);
        }
    }

    public void Merge(Mapper<TKey, TValue> mapper)
    {
        foreach (var (key, value) in mapper._mapping)
        {
            if (!Exists(key))
            {
                _mapping.Add(key, value);
            }
            else if (Exists(key) && TryGet(key)?.Equals(value) == false)
            {
                throw new InvalidOperationException("Incompatible mappings");
            }
        }
    }
}