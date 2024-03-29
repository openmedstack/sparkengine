﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Maintenance;

using System;

internal static class MaintenanceMode
{
    private static readonly object _mutex = new();
    private static volatile MaintenanceLock? _lock;

    /// <summary>
    ///     Whether maintenance mode is enabled. If <code>true</code>
    ///     then all the data modifying requests should be responded
    ///     with <code>503</code> HTTP status code.
    /// </summary>
    public static bool IsEnabled(MaintenanceLockMode mode) => _lock?.IsLocked == true && _lock.Mode >= mode;

    /// <summary>
    ///     Sets maintenance mode ON. The returned lock handle should be used
    ///     to reset the maintenance state.
    /// </summary>
    /// <param name="mode">Lock mode, write only, or read and write</param>
    /// <exception cref="MaintenanceModeEnabledException">Maintenance mode already enabled somewhere else.</exception>
    public static MaintenanceLock Enable(MaintenanceLockMode mode)
    {
        if (_lock?.IsLocked != true)
        {
            lock (_mutex)
            {
                if (_lock?.IsLocked != true)
                {
                    _lock = new MaintenanceLock(mode);
                    return _lock;
                }
            }
        }

        throw new MaintenanceModeEnabledException();
    }

    public static bool IsEnabledForHttpMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException(nameof(method));
        }

        return method.ToUpper() switch
        {
            "GET" => IsEnabled(MaintenanceLockMode.Full),
            "HEAD" => IsEnabled(MaintenanceLockMode.Full),
            "OPTIONS" => IsEnabled(MaintenanceLockMode.Full),
            _ => IsEnabled(MaintenanceLockMode.Write)
        };
    }
}