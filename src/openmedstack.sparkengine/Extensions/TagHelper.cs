// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Extensions;

using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;

public static class TagHelper
{
    private static IEnumerable<Coding> AffixTags(this IEnumerable<Coding> target, IEnumerable<Coding> source)
    {
        // Union works with equality [http://www.healthintersections.com.au/?p=1941]
        // the source should overwrite the existing target tags

        foreach (var s in source.Where(tag => target.Any(t => t.System == tag.System)))
        {
            yield return s;
        }

        foreach (var t in target)
        {
            yield return t;
        }
    }

    private static IEnumerable<Coding> AffixTags(this Meta target, Meta source)
    {
        var targetTags = target.Tag ?? Enumerable.Empty<Coding>();
        var sourceTags = source.Tag ?? Enumerable.Empty<Coding>();
        return targetTags.AffixTags(sourceTags);
    }

    public static void AffixTags(this Resource target, Parameters parameters)
    {
        target.Meta ??= new Meta();

        var meta = parameters.ExtractMeta().FirstOrDefault();
        if (meta != null)
        {
            target.Meta.Tag = AffixTags(target.Meta, meta).ToList();
        }
    }
}
