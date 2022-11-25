namespace OpenMedStack.Linq2Fhir.Provider;

using System;
using System.Linq;
using Hl7.Fhir.Model;

/// <summary>
/// Defines the queryable interface for FHIR services.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFhirQueryable<out T> : IOrderedAsyncQueryable<T>, IAsyncDisposable where T : Resource { }