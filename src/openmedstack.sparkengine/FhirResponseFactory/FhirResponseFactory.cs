// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.FhirResponseFactory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Extensions;
    using Hl7.Fhir.Model;
    using Interfaces;

    public class FhirResponseFactory : IFhirResponseFactory
    {
        private readonly IFhirResponseInterceptorRunner _interceptorRunner;

        public FhirResponseFactory(IFhirResponseInterceptorRunner interceptorRunner)
        {
            _interceptorRunner = interceptorRunner;
        }

        public FhirResponse GetFhirResponse(Entry? entry, IKey? key = null, IEnumerable<object>? parameters = null)
        {
            if (entry == null)
            {
                return Respond.NotFound(key);
            }

            if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            FhirResponse? response = null;

            if (parameters != null)
            {
                response = _interceptorRunner.RunInterceptors(entry, parameters);
            }

            return response ?? Respond.WithResource(entry);
        }

        public FhirResponse GetFhirResponse(Entry? entry, IKey? key = null, params object[] parameters) =>
            GetFhirResponse(entry, key, parameters.ToList());

        public FhirResponse GetMetadataResponse(Entry? entry, IKey? key = null)
        {
            if (entry == null)
            {
                return Respond.NotFound(key);
            }

            if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            return Respond.WithMeta(entry);
        }

        public async Task<FhirResponse> GetFhirResponse(
            IAsyncEnumerable<Tuple<Entry, FhirResponse>> responses,
            Bundle.BundleType bundleType)
        {
            var bundle = new Bundle {Type = bundleType};
            await foreach (var response in responses.ConfigureAwait(false))
            {
                bundle.Append(response.Item1, response.Item2);
            }

            return Respond.WithBundle(bundle);
        }

        public FhirResponse GetFhirResponse(Bundle? bundle) => Respond.WithBundle(bundle);
    }
}