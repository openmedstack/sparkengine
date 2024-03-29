// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.FhirResponseFactory;

using System.Collections.Generic;
using System.Linq;
using Core;
using Interfaces;

public class FhirResponseInterceptorRunner : IFhirResponseInterceptorRunner
{
    private readonly IList<IFhirResponseInterceptor> _interceptors;

    public FhirResponseInterceptorRunner(IFhirResponseInterceptor[] interceptors) =>
        _interceptors = new List<IFhirResponseInterceptor>(interceptors);

    public void AddInterceptor(IFhirResponseInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
    }

    public void ClearInterceptors()
    {
        _interceptors.Clear();
    }

    public FhirResponse? RunInterceptors(ResourceInfo entry, IEnumerable<object> parameters)
    {
        FhirResponse? response = null;
        _ = parameters.FirstOrDefault(p => (response = RunInterceptors(entry, p)) != null);
        return response;
    }

    private FhirResponse? RunInterceptors(ResourceInfo entry, object input)
    {
        FhirResponse? response = null;
        _ = GetResponseInterceptors(input)
            .FirstOrDefault(f => (response = f.GetFhirResponse(entry, input)) != null);
        return response;
    }

    private IEnumerable<IFhirResponseInterceptor> GetResponseInterceptors(object input)
    {
        return _interceptors.Where(i => i.CanHandle(input));
    }
}