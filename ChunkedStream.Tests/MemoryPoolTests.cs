﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ChunkedStream;
using ChunkedStream.Chunks;

namespace ChunkedStream.Tests
{
    [TestClass]
    public class MemoryPoolTests
    {
        [TestMethod]
        public void MemoryPool_ChunkCount()
        {
            var cases = Enumerable.Range(1, 10).Select(i => new { ChunkCount = i, ChunkSize = 4 });

            foreach (var @case in cases)
            {
                var memPool = new MemoryPool(chunkSize: @case.ChunkSize, chunkCount: @case.ChunkCount);

                for (int i = 0; i < @case.ChunkCount; i++)
                {
                    Assert.AreEqual(i, memPool.TryGetChunkHandle());
                }
                Assert.AreEqual(-1, memPool.TryGetChunkHandle());
            }
        }

        [TestMethod]
        public void MemoryPool_InvalidParameters()
        {
            var cases = new[] {
                new { ChunkSize = 0, ChunkCount = 1 },
                new { ChunkSize = -1, ChunkCount = 1},
                new { ChunkSize = 4, ChunkCount = 0},
                new { ChunkSize = 4, ChunkCount = -1},
                new { ChunkSize = 0, ChunkCount = 0},
                new { ChunkSize = -1, ChunkCount = -1},
                new { ChunkSize = Int32.MaxValue, ChunkCount = Int32.MaxValue}};

            foreach (var @case in cases)
            {
                try
                {
                    var memPool = new MemoryPool(chunkSize: @case.ChunkSize, chunkCount: @case.ChunkCount);
                    Assert.Fail("ArgumentException expected to be thrown");
                }
                catch (ArgumentException)
                {
                    // pass
                }
            }
        }

        [TestMethod]
        public void MemoryPool_GetChunk()
        {
            var cases = Enumerable.Range(1, 10).Select(i => new { ChunkCount = i, ChunkSize = 4 });

            foreach (var @case in cases)
            {
                var memPool = new MemoryPool(chunkSize: @case.ChunkSize, chunkCount: @case.ChunkCount);

                for (int i = 0; i < @case.ChunkCount; i++)
                {
                    Assert.IsInstanceOfType(memPool.GetChunk(), typeof(MemoryPoolChunk));
                }
                Assert.IsInstanceOfType(memPool.GetChunk(), typeof(MemoryChunk));
                Assert.AreEqual(@case.ChunkCount, memPool.TotalAllocated);
            }
        }

        [TestMethod]
        public void MemoryPool_TryGetChunkFromPool_AfterRelease()
        {
            var cases = Enumerable.Range(1, 10).Select(i => new { ChunkCount = i, ChunkSize = 4 });

            foreach (var @case in cases)
            {
                var memPool = new MemoryPool(chunkSize: @case.ChunkSize, chunkCount: @case.ChunkCount);

                var chunks = new List<IChunk>(@case.ChunkCount);

                for (int i = 0; i < @case.ChunkCount; i++)
                {
                    var chunk = memPool.GetChunk();
                    Assert.IsInstanceOfType(chunk, typeof(MemoryPoolChunk));
                    chunks.Add(chunk);
                }
                Assert.IsNull(memPool.TryGetChunkFromPool());
                Assert.AreEqual(@case.ChunkCount, memPool.TotalAllocated);

                chunks.ForEach(chunk => chunk.Dispose());
                Assert.AreEqual(0, memPool.TotalAllocated);

                for (int i = 0; i < @case.ChunkCount; i++)
                {
                    var chunk = memPool.GetChunk();
                    Assert.IsInstanceOfType(chunk, typeof(MemoryPoolChunk));
                }
                Assert.IsNull(memPool.TryGetChunkFromPool());
                Assert.AreEqual(@case.ChunkCount, memPool.TotalAllocated);
            }
        }

        [TestMethod]
        public void MemoryPool_TryGetChunkFromPool()
        {
            var cases = Enumerable.Range(1, 10).Select(i => new { ChunkCount = i, ChunkSize = 4 });

            foreach (var @case in cases)
            {
                var memPool = new MemoryPool(chunkSize: @case.ChunkSize, chunkCount: @case.ChunkCount);

                for (int i = 0; i < @case.ChunkCount; i++)
                {
                    Assert.IsInstanceOfType(memPool.TryGetChunkFromPool(), typeof(MemoryPoolChunk));
                }
                Assert.IsNull(memPool.TryGetChunkFromPool());
                Assert.AreEqual(@case.ChunkCount, memPool.TotalAllocated);
            }
        }

        [TestMethod]
        public void MemoryPool_TryGetChunkHandle()
        {
            var memPool = new MemoryPool(chunkSize: 4, chunkCount: 2);

            int handle1 = memPool.TryGetChunkHandle();
            int handle2 = memPool.TryGetChunkHandle();

            Assert.AreEqual(0, handle1);
            Assert.AreEqual(1, handle2);
            Assert.AreEqual(-1, memPool.TryGetChunkHandle());

            memPool.ReleaseChunkHandle(ref handle1);
            Assert.AreEqual(-1, handle1);
            Assert.AreEqual(1, memPool.TotalAllocated);
            Assert.AreEqual(0, memPool.TryGetChunkHandle());
            Assert.AreEqual(-1, memPool.TryGetChunkHandle());

            memPool.ReleaseChunkHandle(ref handle2);
            Assert.AreEqual(-1, handle2);
            Assert.AreEqual(1, memPool.TotalAllocated);
            Assert.AreEqual(1, memPool.TryGetChunkHandle());
            Assert.AreEqual(-1, memPool.TryGetChunkHandle());

            Assert.AreEqual(2, memPool.TotalAllocated);
        }

        [TestMethod]
        public void MemoryPool_ReleaseInvalidHandler()
        {
            var memPool = new MemoryPool(chunkSize: 4, chunkCount: 2);

            foreach (var i in new[] { -1, 2, 3, 100 })
            {
                int handle = i;
                try
                {
                    memPool.ReleaseChunkHandle(ref handle);
                    Assert.Fail("ArgumentException expected to be thrown");
                }
                catch (InvalidOperationException)
                {
                    // pass
                }
            }
        }

        [TestMethod]
        public void MemoryPool_InParallel()
        {
            int threadCount = 8, attempts = 100, num = 100;

            var memPool = new MemoryPool(chunkSize: 4, chunkCount: threadCount / 2);

            var actions = Enumerable.Range(0, threadCount).Select(_ => new Action(() =>
            {
                for (int i = 0; i < attempts; i++)
                {
                    using (var chunk = memPool.GetChunk())
                    {
                        // initialize
                        BitConverter.GetBytes((int)0).CopyTo(chunk.Buffer, chunk.Offset);
                        for (int j = 0; j < num; j++)
                        {
                            // increment
                            BitConverter.GetBytes(BitConverter.ToInt32(chunk.Buffer, chunk.Offset) + 1).CopyTo(chunk.Buffer, chunk.Offset);
                        }
                        // assert
                        Assert.AreEqual(num, BitConverter.ToInt32(chunk.Buffer, chunk.Offset));
                    }
                }
            })).ToArray();

            Parallel.Invoke(actions);
            Assert.AreEqual(0, memPool.TotalAllocated);
        }
    }
}
