namespace RsyncNet.Libraries
{
    using System;
    using System.IO;
    using Helpers;

    public class SlidingStreamBuffer
    {
        private readonly byte[] _buffer;
        private readonly int _extraBuffer;
        private readonly Stream _stream;
        private readonly int _window;
        private int _bufferPosition;
        private int _totalValidBytesInBuffer;
        private bool _warmupCalled;

        public SlidingStreamBuffer(Stream stream, int window)
            : this(stream, window, window)
        {
        }

        public SlidingStreamBuffer(Stream stream, int window, int extraBuffer)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (window < 1) throw new ArgumentException("window must be > 0");
            _bufferPosition = 0;
            _totalValidBytesInBuffer = 0;
            _warmupCalled = false;
            _stream = stream;
            _window = window;
            _extraBuffer = extraBuffer;
            _buffer = new byte[window + extraBuffer];
        }

        #region Properties, indexers, events and operators: public

        /// <summary>
        /// Returns the byte at position i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public byte this[int i]
        {
            get { return GetByteAt(i); }
        }

        #endregion

        #region Methods: public

        public byte[] GetBuffer()
        {
            if (!_warmupCalled) Warmup();
            // wait for async read, if any, to complete
            var retBuffer = new byte[GetNumBytesAvailable()];
            Array.Copy(_buffer, _bufferPosition, retBuffer, 0, retBuffer.Length);
            return retBuffer;
        }

        /// <summary>
        /// Gets a byte at an offset between [0, window>
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public byte GetByteAt(int offset)
        {
            if (!_warmupCalled) Warmup();
            // wait for async read, if any, to complete
            if (offset < 0) throw new IndexOutOfRangeException();
            if (offset > GetNumBytesAvailable() - 1) throw new IndexOutOfRangeException();
            return _buffer[offset + _bufferPosition];
        }

        /// <summary>
        ///  Returns value between [0, window]
        /// </summary>
        /// <returns></returns>
        public long GetNumBytesAvailable()
        {
            return MathEx.Bounded(0, _window, _totalValidBytesInBuffer - _bufferPosition);
        }

        /// <summary>
        /// Slides the window n byte(s) forward
        /// </summary>
        public void MoveForward(int n)
        {
            if (n <= 0) throw new ArgumentException("n must be a positive number larger than zero");
            _bufferPosition += n;
            if (_totalValidBytesInBuffer - _bufferPosition < _window)
            {
                // begin data fetch
                LeftShiftBuffer();
                FillBuffer();
            }
        }

        /// <summary>
        /// Pulls some initial data from the stream
        /// </summary>
        public void Warmup()
        {
            FillBuffer();
            _warmupCalled = true;
        }

        #endregion

        #region Methods: private

        private void FillBuffer()
        {
            int bytesToRead = (_window + _extraBuffer) - _totalValidBytesInBuffer;
            if (bytesToRead <= 0) return;
            int read = _stream.Read(_buffer, _totalValidBytesInBuffer, bytesToRead);
            _totalValidBytesInBuffer += read;
        }

        private void LeftShiftBuffer()
        {
            if (_bufferPosition < _totalValidBytesInBuffer)
            {
                Array.Copy(_buffer, _bufferPosition, _buffer, 0, _totalValidBytesInBuffer - _bufferPosition);
                _totalValidBytesInBuffer -= _bufferPosition;
            }
            else
            {
                _totalValidBytesInBuffer = 0;
            }
            _bufferPosition = 0;
        }

        #endregion
    }
}