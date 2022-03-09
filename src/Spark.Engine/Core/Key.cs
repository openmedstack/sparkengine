// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using Extensions;

    // BALLOT: ResourceId is in the standard called "Logical Id" but this term doesn't have a lot of meaning. I propose "Technical Id" or "Surrogate key"
    // http://en.wikipedia.org/wiki/Surrogate_key

    public class Key : IKey
    {
        public Key()
        {
        }

        public Key(string @base, string type, string resourceId, string versionId)
        {
            Base = @base?.TrimEnd('/');
            TypeName = type;
            ResourceId = resourceId;
            VersionId = versionId;
        }
        
        public string Base { get; set; }
        public string TypeName { get; set; }
        public string ResourceId { get; set; }
        public string VersionId { get; set; }

        public static Key Create(string type) => new(null, type, null, null);

        public static Key Create(string type, string resourceId) => new(null, type, resourceId, null);

        public static Key Create(string type, string resourceId, string versionId) =>
            new(null, type, resourceId, versionId);

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Key) obj;
            return this.ToUriString() == other.ToUriString();
        }

        public override int GetHashCode() => this.ToUriString().GetHashCode();

        public static Key ParseOperationPath(string path)
        {
            var key = new Key();
            path = path.Trim('/');
            var indexOfQueryString = path.IndexOf('?');
            if (indexOfQueryString >= 0)
            {
                path = path.Substring(0, indexOfQueryString);
            }

            var segments = path.Split('/');
            if (segments.Length >= 1)
            {
                key.TypeName = segments[0];
            }

            if (segments.Length >= 2)
            {
                key.ResourceId = segments[1];
            }

            if (segments.Length == 4 && segments[2] == "_history")
            {
                key.VersionId = segments[3];
            }

            return key;
        }

        public override string ToString() => this.ToUriString();
    }
}