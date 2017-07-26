﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChunkedStream
{
    public sealed class MemoryPoolChunk : IChunk
    {
        private readonly MemoryPool _owner;
        private readonly int _offset;
        private int _handle;

        public byte[] Buffer
        {
            get
            {
                return _owner.Buffer;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
        }

        public int Length
        {
            get
            {
                return _owner.ChunkSize;
            }
        }

        public MemoryPoolChunk(MemoryPool owner, int handle)
        {
            _owner = owner;
            _handle = handle;
            _offset = owner.GetChunkOffset(handle);
        }

        private void ReleaseChunkHandle()
        {
            if (_handle != MemoryPool.InvalidHandler)
            {
                _owner.ReleaseChunkHandle(ref _handle);
            }
        }

        public void Dispose()
        {
            ReleaseChunkHandle();
            GC.SuppressFinalize(this);
        }

        ~MemoryPoolChunk()
        {
            ReleaseChunkHandle();

        }
    }
}
