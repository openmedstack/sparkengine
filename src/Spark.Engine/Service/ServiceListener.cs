// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;

    public class ServiceListener : ICompositeServiceListener
    {
        private readonly List<IServiceListener> _listeners;
        private readonly ILocalhost _localhost;

        public ServiceListener(ILocalhost localhost, IServiceListener[] listeners = null)
        {
            _localhost = localhost;
            if (listeners != null)
            {
                _listeners = listeners.ToList();
            }
        }

        public void Add(IServiceListener listener)
        {
            _listeners.Add(listener);
        }

        public void Clear()
        {
            _listeners.Clear();
        }

        public Task Inform(Entry interaction)
        {
            return Task.WhenAll(
                _listeners.Select(
                    listener => listener.Inform(_localhost.GetAbsoluteUri(interaction.Key), interaction)));
        }

        public Task Inform(Uri location, Entry entry)
        {
            return Task.WhenAll(_listeners.Select(listener => listener.Inform(location, entry)));
        }
        }
}
