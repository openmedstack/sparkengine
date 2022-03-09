// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test
{
    using System.IO;

    public static class TextFileHelper
    {
        public static string ReadTextFileFromDisk(string path)
        {
            using TextReader reader = new StreamReader(path);
            return reader.ReadToEnd();
        }
    }
}