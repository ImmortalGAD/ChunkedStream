﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChunkedStream.Chunks
{
    public sealed class MemoryChunk : IChunk
    {
        private readonly byte[] _buffer;

        public byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public int Offset
        {
            get
            {
                return 0;
            }
        }

        public int Length
        {
            get
            {
                return _buffer.Length;
            }
        }

        public MemoryChunk(int chunkSize)
        {
            if (chunkSize <= 0 || chunkSize > MemoryPool.MaxChunkSize)
                throw new ArgumentException($"chunkSize must be positive and less than or equal 2^30", "chunkSize");

            _buffer = new byte[chunkSize];
        }

        public void Dispose()
        {
        }
    }
}
