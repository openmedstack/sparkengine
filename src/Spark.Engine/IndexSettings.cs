// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine
{
    public class IndexSettings
    {
        /// <summary>
        ///     Whether to clear index before rebuilding it. Setting it to <code>false</code> (default)
        ///     will may cause stale records to appear in index (for example, when some documents are not
        ///     reindexed for some reason).
        /// </summary>
        public bool ClearIndexOnRebuild { get; set; }

        /// <summary>
        ///     Number of documents to be loaded into memory for reindexing.
        ///     It's recommended to keep it low for having the low memory footprint.
        /// </summary>
        public int ReindexBatchSize { get; set; } = 100;
    }
}