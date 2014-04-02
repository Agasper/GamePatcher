namespace RsyncNet.Delta
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Helpers;

    public class DeltaStreamer
    {
        private int _streamChunkSize;

        public DeltaStreamer()
        {
            StreamChunkSize = 16384;
        }

        #region Properties, indexers, events and operators: public

        public int StreamChunkSize
        {
            get { return _streamChunkSize; }
            set
            {
                if (value <= 0) throw new ArgumentException("value must be a positive number greater than 0");
                _streamChunkSize = value;
            }
        }

        #endregion

        #region Methods: public

        /// <summary>
        /// Reconstructs remote data, given a delta stream and a random access / seekable input stream,
        /// all written to outputStream.
        /// </summary>
        /// <param name="deltaStream">sequential stream of deltas</param>
        /// <param name="inputStream">seekable and efficiently random access stream</param>
        /// <param name="outputStream">sequential stream for output</param>
        public void Receive(Stream deltaStream, Stream inputStream, Stream outputStream)
        {
            if (deltaStream == null) throw new ArgumentNullException("deltaStream");
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");
            if (inputStream.CanSeek == false) throw new InvalidOperationException("inputStream must be seekable");

            int commandByte;
            while ((commandByte = deltaStream.ReadByte()) != -1)
            {
                if (commandByte == DeltaStreamConstants.NEW_BLOCK_START_MARKER)
                {
                    int length = deltaStream.ReadInt();
                    var buffer = new byte[length];
                    deltaStream.Read(buffer, 0, length);
                    outputStream.Write(buffer, 0, length);
                }
                else if (commandByte == DeltaStreamConstants.COPY_BLOCK_START_MARKER)
                {
                    long sourceOffset = deltaStream.ReadLong();
                    int length = deltaStream.ReadInt();
                    var buffer = new byte[length];
                    inputStream.Seek(sourceOffset, SeekOrigin.Begin);
                    inputStream.Read(buffer, 0, length);
                    outputStream.Write(buffer, 0, length);
                }
                else throw new IOException("Invalid data found in deltaStream");
            }
        }

        public void Send(IEnumerable<IDelta> deltas, Stream inputStream, Stream outputStream)
        {
            if (deltas == null) throw new ArgumentNullException("deltas");
            if (deltas.Count() == 0) throw new ArgumentException("'deltas' must have one or more IDelta objects");
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            foreach (IDelta delta in deltas)
            {
                if (delta is ByteDelta)
                {
                    SendByteDelta(delta as ByteDelta, inputStream, outputStream);
                }
                else if (delta is CopyDelta)
                {
                    SendCopyDelta(delta as CopyDelta, inputStream, outputStream);
                }
            }
        }

        #endregion

        #region Methods: private

        private void SendByteDelta(ByteDelta delta, Stream inputStream, Stream outputStream)
        {
            outputStream.WriteByte(DeltaStreamConstants.NEW_BLOCK_START_MARKER);
            outputStream.WriteInt(delta.Length);
            var buffer = new byte[delta.Length];
            inputStream.Seek(delta.Offset, SeekOrigin.Begin);
            long totalRead = 0;
            while (totalRead < delta.Length)
            {
                var toRead = (int) MathEx.Bounded(0, StreamChunkSize, delta.Length - totalRead);
                int readLength = inputStream.Read(buffer, 0, toRead);
                if (readLength == 0 && totalRead < delta.Length)
                    throw new IOException("Input stream offset out of bounds, or not enough data available");
                outputStream.Write(buffer, 0, readLength);
                totalRead += readLength;
            }
        }

        private void SendCopyDelta(CopyDelta delta, Stream inputStream, Stream outputStream)
        {
            if (inputStream.CanSeek == false) throw new IOException("inputStream not seekable");
            outputStream.WriteByte(DeltaStreamConstants.COPY_BLOCK_START_MARKER);
            outputStream.WriteLong(delta.Offset);
            outputStream.WriteInt(delta.Length);
            inputStream.Seek(delta.Length, SeekOrigin.Current);
        }

        #endregion

        #region Nested type: DeltaStreamConstants

        internal static class DeltaStreamConstants
        {
            #region Fields: public

            public static byte COPY_BLOCK_START_MARKER = (byte) 'C';
            public static byte NEW_BLOCK_START_MARKER = (byte) 'N';

            #endregion
        }

        #endregion
    }
}