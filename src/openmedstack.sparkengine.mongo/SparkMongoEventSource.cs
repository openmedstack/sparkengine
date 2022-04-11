// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo
{
    using System;
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Furore-Spark-Mongo")]
    public sealed class SparkMongoEventSource : EventSource
    {
        //public class Tasks
        //{
        //    public const EventTask ServiceMethod = (EventTask)1;
        //}

        private static readonly Lazy<SparkMongoEventSource> _instance =
            new Lazy<SparkMongoEventSource>(() => new SparkMongoEventSource());

        private SparkMongoEventSource()
        {
        }

        public static SparkMongoEventSource Log => _instance.Value;

        [Event(1, Message = "Method call: {0}", Level = EventLevel.Verbose, Keywords = Keywords.TRACING)]
        internal void ServiceMethodCalled(string methodName)
        {
            WriteEvent(1, methodName);
        }

        public class Keywords
        {
            public const EventKeywords TRACING = (EventKeywords) 1;
            public const EventKeywords UNSUPPORTED = (EventKeywords) 2;
        }
    }
}