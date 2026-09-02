using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadTable
    {
        private const int MaxThreads = 4096;
        private const int FastTableSize = 8192;

        public const int FastTableMask = FastTableSize - 1;

        private struct Entry
        {
            public nint ThreadId;
            public nint NativeContextPtr;

            public Entry(nint threadId, nint nativeContextPtr)
            {
                ThreadId = threadId;
                NativeContextPtr = nativeContextPtr;
            }
        }

        private static MemoryBlock _block;
        private static MemoryBlock _fastBlock;

        public static nint EntriesPointer => _block.Pointer + 8;
        public static nint FastEntriesPointer => _fastBlock.Pointer;

        static NceThreadTable()
        {
            _block = new MemoryBlock((ulong)Unsafe.SizeOf<Entry>() * MaxThreads + 8UL);
            _block.Write(0UL, 0UL);

            // MemoryBlock storage is zero-initialized, just like the existing table.
            // The direct-mapped cache is only a hot-path accelerator: collisions
            // always fall back to the original bounded table and cannot affect
            // correctness.
            _fastBlock = new MemoryBlock((ulong)Unsafe.SizeOf<Entry>() * FastTableSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFastIndex(nint threadId)
        {
            ulong value = (ulong)(nuint)threadId;
            value ^= value >> 17;
            return (int)((value >> 4) & FastTableMask);
        }

        public static int Register(nint threadId, nint nativeContextPtr)
        {
            Span<Entry> entries = GetStorage();

            lock (_block)
            {
                ref ulong currentThreadCount = ref GetThreadsCount();

                for (int i = 0; i < MaxThreads; i++)
                {
                    if (entries[i].ThreadId == nint.Zero)
                    {
                        entries[i] = new Entry(threadId, nativeContextPtr);

                        ref Entry fastEntry = ref GetFastStorage()[GetFastIndex(threadId)];
                        fastEntry.NativeContextPtr = nativeContextPtr;
                        Thread.MemoryBarrier();
                        fastEntry.ThreadId = threadId;

                        if (currentThreadCount < (ulong)i + 1)
                        {
                            currentThreadCount = (ulong)i + 1;
                        }

                        return i;
                    }
                }
            }

            throw new Exception($"Number of active threads exceeds limit of {MaxThreads}.");
        }

        public static void Unregister(int tableIndex)
        {
            Span<Entry> entries = GetStorage();

            lock (_block)
            {
                nint threadId = entries[tableIndex].ThreadId;

                if (threadId != nint.Zero)
                {
                    ref Entry fastEntry = ref GetFastStorage()[GetFastIndex(threadId)];

                    // A colliding registration may have replaced this slot. Never
                    // clear a cache entry that now belongs to another thread.
                    if (fastEntry.ThreadId == threadId)
                    {
                        fastEntry.ThreadId = nint.Zero;
                        Thread.MemoryBarrier();
                        fastEntry.NativeContextPtr = nint.Zero;
                    }

                    entries[tableIndex] = default;

                    ulong currentThreadCount = GetThreadsCount();

                    for (int i = (int)currentThreadCount - 1; i >= 0; i--)
                    {
                        if (entries[i].ThreadId != nint.Zero)
                        {
                            break;
                        }

                        currentThreadCount = (ulong)i;
                    }

                    GetThreadsCount() = currentThreadCount;
                }
            }
        }

        private static ref ulong GetThreadsCount()
        {
            return ref _block.GetRef<ulong>(0UL);
        }

        private static unsafe Span<Entry> GetStorage()
        {
            return new Span<Entry>((void*)_block.GetPointer(8UL, (ulong)Unsafe.SizeOf<Entry>() * MaxThreads), MaxThreads);
        }

        private static unsafe Span<Entry> GetFastStorage()
        {
            return new Span<Entry>((void*)_fastBlock.GetPointer(0UL, (ulong)Unsafe.SizeOf<Entry>() * FastTableSize), FastTableSize);
        }
    }
}
