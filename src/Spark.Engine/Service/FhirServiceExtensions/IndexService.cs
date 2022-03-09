/* 
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */
namespace Spark.Engine.Service.FhirServiceExtensions
{
    using Hl7.Fhir.FhirPath;
    using Hl7.Fhir.Model;
    using Spark.Engine.Core;
    using Spark.Engine.Extensions;
    using Spark.Engine.Model;
    using Spark.Engine.Search;
    using Spark.Engine.Search.Model;
    using Spark.Engine.Store.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Search.ValueExpressionTypes;
    using Task = System.Threading.Tasks.Task;

    public class IndexService : IIndexService
    {
        private readonly ElementIndexer _elementIndexer;
        private readonly IFhirModel _fhirModel;
        private readonly IIndexStore _indexStore;

        public IndexService(IFhirModel fhirModel, IIndexStore indexStore, ElementIndexer elementIndexer)
        {
            _fhirModel = fhirModel;
            _indexStore = indexStore;
            _elementIndexer = elementIndexer;
        }

        public async Task Process(Entry entry)
        {
            if (entry.HasResource())
            {
                await IndexResource(entry.Resource, entry.Key).ConfigureAwait(false);
            }
            else
            {
                if (entry.IsDeleted())
                {
                    await _indexStore.Delete(entry).ConfigureAwait(false);
                }
                else
                {
                    throw new Exception("Entry is neither resource nor deleted");
                }
            }
        }

        public async Task<IndexValue> IndexResource(Resource resource, IKey key)
        {
            var resourceToIndex = MakeContainedReferencesUnique(resource);
            var indexValue = IndexResourceRecursively(resourceToIndex, key);
            await _indexStore.Save(indexValue).ConfigureAwait(false);
            return indexValue;
        }

        private IndexValue IndexResourceRecursively(Resource resource, IKey key, string rootPartName = "root")
        {
            var searchParameters = _fhirModel.FindSearchParameters(resource.GetType());

            if (searchParameters == null)
            {
                return null;
            }

            var rootIndexValue = new IndexValue(rootPartName);
            AddMetaParts(resource, key, rootIndexValue);

            ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();

            foreach (var searchParameter in searchParameters)
            {
                if (string.IsNullOrWhiteSpace(searchParameter.Expression))
                {
                    continue;
                }

                // TODO: Do we need to index composite search parameters, some
                // of them are already indexed by ordinary search parameters so
                // need to make sure that we don't do overlapping indexing.
                if (searchParameter.Type == Hl7.Fhir.Model.SearchParamType.Composite)
                {
                    continue;
                }

                var indexValue = new IndexValue(searchParameter.Code);
                IEnumerable<Base> resolvedValues;
                // HACK: Ignoring search parameter expressions which the FhirPath engine does not yet have support for
                try
                {
                    resolvedValues = resource.SelectNew(searchParameter.Expression);
                }
                catch// (Exception e)
                {
                    // TODO: log error!
                    resolvedValues = new List<Base>();
                }

                foreach (var value in resolvedValues)
                {
                    if (value is not Element element)
                    {
                        continue;
                    }

                    indexValue.Values.AddRange(_elementIndexer.Map(element));
                }

                if (indexValue.Values.Any())
                {
                    rootIndexValue.Values.Add(indexValue);
                }
            }

            if (resource is DomainResource domainResource)
            {
                AddContainedResources(domainResource, rootIndexValue);
            }

            return rootIndexValue;
        }

        /// <summary>
        ///     The id of a contained resource is only unique in the context of its 'parent'.
        ///     We want to allow the indexStore implementation to treat the IndexValue that comes from the contained resources just
        ///     like a regular resource.
        ///     Therefore we make the id's globally unique, and adjust the references that point to it from its 'parent'
        ///     accordingly.
        ///     This method trusts on the knowledge that contained resources cannot contain any further nested resources. So one
        ///     level deep only.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>A copy of resource, with id's of contained resources and references in resource adjusted to unique values.</returns>
        private static Resource MakeContainedReferencesUnique(Resource resource)
        {
            //We may change id's of contained resources, and don't want that to influence other code. So we make a copy for our own needs.
            Resource result = (dynamic)resource.DeepCopy();
            if (resource is DomainResource)
            {
                var domainResource = (DomainResource)result;
                if (domainResource.Contained != null && domainResource.Contained.Any())
                {
                    var referenceMap = new Dictionary<string, string>();

                    // Create a unique id for each contained resource.
                    foreach (var containedResource in domainResource.Contained)
                    {
                        var oldRef = "#" + containedResource.Id;
                        var newId = Guid.NewGuid().ToString();
                        containedResource.Id = newId;
                        var newRef = containedResource.TypeName + "/" + newId;
                        referenceMap.Add(oldRef, newRef);
                    }

                    // Replace references to these contained resources with the newly created id's.
                    Auxiliary.ResourceVisitor.VisitByType(
                        domainResource,
                        (el, path) =>
                        {
                            var currentRefence = el as ResourceReference;
                            if (!string.IsNullOrEmpty(currentRefence.Reference))
                            {
                                referenceMap.TryGetValue(currentRefence.Reference, out var replacementId);
                                if (replacementId != null)
                                {
                                    currentRefence.Reference = replacementId;
                                }
                            }
                        },
                        typeof(ResourceReference));
                }
            }

            return result;
        }

        private void AddContainedResources(DomainResource resource, IndexValue parent)
        {
            parent.Values.AddRange(
                resource.Contained.Where(c => c is DomainResource)
                    .Select(
                        c =>
                        {
                            IKey containedKey = c.ExtractKey();
                            return IndexResourceRecursively(c as DomainResource, containedKey, "contained");
                        }));
        }

        private static void AddMetaParts(Resource resource, IKey key, IndexValue entry)
        {
            entry.Values.Add(new IndexValue("internal_forResource", new StringValue(key.ToUriString())));
            entry.Values.Add(new IndexValue(IndexFieldNames.RESOURCE, new StringValue(resource.TypeName)));
            entry.Values.Add(
                new IndexValue(IndexFieldNames.ID, new StringValue(resource.TypeName + "/" + key.ResourceId)));
            entry.Values.Add(new IndexValue(IndexFieldNames.JUSTID, new StringValue(resource.Id)));
            entry.Values.Add(
                new IndexValue(
                    IndexFieldNames.SELFLINK,
                    new StringValue(
                        key.ToUriString()))); //CK TODO: This is actually Mongo-specific. Move it to Spark.Mongo, but then you will have to communicate the key to the MongoIndexMapper.
        }
    }
}