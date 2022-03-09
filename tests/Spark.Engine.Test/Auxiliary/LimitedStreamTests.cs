// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Auxiliary
{
    using System;
    using System.IO;
    using Engine.Auxiliary;
    using Xunit;

    public class LimitedStreamTests
    {
        [Fact]
        public void TestWriteWithinLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 10);

            sut.Write(new byte[5] {1, 2, 3, 4, 5}, 0, 5);

            var actual = new byte[5];
            innerStream.Seek(0, SeekOrigin.Begin);
            innerStream.Read(actual, 0, 5);

            Assert.Equal((byte) 1, actual[0]);
            Assert.Equal((byte) 5, actual[4]);
        }

        [Fact]
        public void TestWriteAboveLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write(new byte[5] {1, 2, 3, 4, 5}, 0, 5));
        }

        [Fact]
        public void TestWriteWithinThenAboveLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 10);

            sut.Write(new byte[5] {1, 2, 3, 4, 5}, 0, 5);

            var actual5 = new byte[5];
            innerStream.Seek(0, SeekOrigin.Begin);
            innerStream.Read(actual5, 0, 5);

            Assert.Equal((byte) 1, actual5[0]);
            Assert.Equal((byte) 5, actual5[4]);

            sut.Write(new byte[5] {6, 7, 8, 9, 10}, 0, 5);

            var actual10 = new byte[10];
            innerStream.Seek(0, SeekOrigin.Begin);
            innerStream.Read(actual10, 0, 10);

            Assert.Equal((byte) 1, actual10[0]);
            Assert.Equal((byte) 10, actual10[9]);

            Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write(new byte[1] {11}, 0, 1));
        }

        [Fact]
        public void TestWriteWithinLimitWithOffset()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 3);

            sut.Write(new byte[5] {1, 2, 3, 4, 5}, 2, 3);

            var actual3 = new byte[3];
            innerStream.Seek(0, SeekOrigin.Begin);
            innerStream.Read(actual3, 0, 3);

            Assert.Equal((byte) 3, actual3[0]);
            Assert.Equal((byte) 5, actual3[2]);
        }

        [Fact]
        public void TestWriteAboveLimitWithByteLengthShorterThanCount()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write(new byte[5] {1, 2, 3, 4, 5}, 1, 13));
        }

        [Fact]
        public void TestCopyToWithinLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 5);

            var sourceStream = new MemoryStream(new byte[5] {1, 2, 3, 4, 5});

            sourceStream.CopyTo(sut);

            var actual = new byte[5];
            innerStream.Seek(0, SeekOrigin.Begin);
            innerStream.Read(actual, 0, 5);

            Assert.Equal((byte) 1, actual[0]);
            Assert.Equal((byte) 5, actual[4]);
        }

        [Fact]
        public void TestCopyToAboveLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 3);

            var sourceStream = new MemoryStream(new byte[5] {1, 2, 3, 4, 5});

            Assert.Throws<ArgumentOutOfRangeException>(() => sourceStream.CopyTo(sut));
        }

        [Fact]
        public void TestCopyToAsyncAboveLimit()
        {
            var innerStream = new MemoryStream();
            var sut = new LimitedStream(innerStream, 3);

            var sourceStream = new MemoryStream(new byte[5] {1, 2, 3, 4, 5});

            try
            {
                var t = sourceStream.CopyToAsync(sut);
                t.Wait();
            }
            catch (AggregateException ae)
            {
                Assert.IsType<ArgumentOutOfRangeException>(ae.InnerException);
            }
        }
    }
}