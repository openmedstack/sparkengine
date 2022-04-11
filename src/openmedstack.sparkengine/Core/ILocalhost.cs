/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Core
{
    using System;

    public interface ILocalhost
    {
        Uri DefaultBase { get; }
        Uri Absolute(Uri uri);
        bool IsBaseOf(Uri uri);
        Uri? GetBaseOf(Uri uri);
    }
    
}
