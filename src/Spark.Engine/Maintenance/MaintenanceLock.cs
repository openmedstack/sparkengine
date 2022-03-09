// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Maintenance
{
    using System;

    internal class MaintenanceLock : IDisposable
    {
        public MaintenanceLock(MaintenanceLockMode mode) => Mode = mode;

        public MaintenanceLockMode Mode { get; private set; }

        public bool IsLocked => Mode > MaintenanceLockMode.None;

        public void Dispose()
        {
            Unlock();
        }

        public void Unlock()
        {
            Mode = MaintenanceLockMode.None;
        }
    }
}