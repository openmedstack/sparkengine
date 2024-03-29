﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search.Support;

using System;
using System.Globalization;

/// <summary>
///     Utility class for creating and unwrapping <see cref="Exception" /> instances.
/// </summary>
internal static class Error
{
    /// <summary>
    ///     Formats the specified resource string using <see cref="M:CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <returns>The formatted string.</returns>
    private static string FormatMessage(string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    /// <summary>
    ///     Creates an <see cref="ArgumentException" /> with the provided properties.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
    /// <param name="messageFormat">A composite format string explaining the reason for the exception.</param>
    /// <param name="messageArgs">An object array that contains zero or more objects to format.</param>
    /// <returns>The logged <see cref="Exception" />.</returns>
    internal static ArgumentException Argument(
        string parameterName,
        string messageFormat,
        params object[] messageArgs) =>
        new(FormatMessage(messageFormat, messageArgs), parameterName);

    /// <summary>
    ///     Creates an <see cref="ArgumentNullException" /> with the provided properties.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
    /// <returns>The logged <see cref="Exception" />.</returns>
    internal static ArgumentException ArgumentNull(string parameterName) =>
        new ArgumentNullException(parameterName);
}