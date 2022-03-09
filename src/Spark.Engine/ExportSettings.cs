// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine
{
    public class ExportSettings
    {
        /// <summary>
        ///     Whether to externalize FHIR URIs, for example, <code>"Patient"</code> ->
        ///     <code>"https://your.fhir.url/fhir/Patient"</code> (<code>false</code> by default).
        /// </summary>
        public bool ExternalizeFhirUri { get; set; }
    }
}