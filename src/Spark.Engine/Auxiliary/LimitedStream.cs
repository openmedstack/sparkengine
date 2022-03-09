// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Auxiliary
{
    using System;
    using System.IO;

    public class LimitedStream : Stream
    {
        private readonly Stream _innerStream = null;

        /// <summary>
        ///     Creates a write limit on the underlying <paramref name="stream" /> of <paramref name="sizeLimitInBytes" />, which
        ///     has a default of 2048 (2kB).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="sizeLimitInBytes"></param>
        public LimitedStream(Stream stream, long sizeLimitInBytes = 2048)
        {
            _innerStream = stream ?? throw new ArgumentNullException(nameof(stream), "stream cannot be null");
            SizeLimitInBytes = sizeLimitInBytes;
        }

        public long SizeLimitInBytes { get; }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite && _innerStream.Length < SizeLimitInBytes;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;

            set => _innerStream.Position = value;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesToBeAdded = Math.Min(buffer.Length - offset, count);
            if (Length + bytesToBeAdded > SizeLimitInBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(buffer),
                    $"Adding {bytesToBeAdded} bytes to the stream would exceed the size limit of {SizeLimitInBytes} bytes.");
            }

            _innerStream.Write(buffer, offset, count);
        }
    }
}