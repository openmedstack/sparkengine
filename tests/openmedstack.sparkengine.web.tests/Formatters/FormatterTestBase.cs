﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web.Tests.Formatters;

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class FormatterTestBase
{
    protected string GetResourceFromFileAsString(string path)
    {
        using TextReader reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    protected static HttpContext GetHttpContext(byte[] contentBytes, string? contentType) =>
        GetHttpContext(new MemoryStream(contentBytes), contentType);

    protected static HttpContext GetHttpContext(Stream requestStream, string? contentType)
    {
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Body = requestStream,
                ContentType = contentType
            }
        };

        return httpContext;
    }

    protected static InputFormatterContext CreateInputFormatterContext(
        Type modelType,
        HttpContext httpContext,
        string? modelName = null,
        bool treatEmptyInputAsDefaultValue = false)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);

        return new InputFormatterContext(
            httpContext,
            modelName ?? string.Empty,
            new ModelStateDictionary(),
            metadata,
            new TestHttpRequestStreamReaderFactory().CreateReader,
            treatEmptyInputAsDefaultValue);
    }
}
