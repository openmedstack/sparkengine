namespace Spark.Engine.Web.Tests.Persistence
{
    using System;
    using System.Collections.Generic;

    public class IndexEntry : IEquatable<IndexEntry>
    {
        public IndexEntry(string id, string canonicalId, string resourceType, Dictionary<string, object> values)
        {
            Id = id;
            CanonicalId = canonicalId;
            ResourceType = resourceType;
            Values = values;
        }

        public string Id { get; }

        public string CanonicalId { get; }

        public string ResourceType { get; }

        public Dictionary<string, object> Values { get; }

        /// <inheritdoc />
        public bool Equals(IndexEntry other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id && Equals(Values, other.Values);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == this.GetType() && Equals((IndexEntry)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Values);
        }
    }
}