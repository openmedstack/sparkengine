﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Interfaces;

using System.Threading.Tasks;
using Core;

public interface ICompositeServiceListener : IServiceListener
{
    void Add(IServiceListener listener);
    void Clear();
    Task Inform(Entry interaction);
}