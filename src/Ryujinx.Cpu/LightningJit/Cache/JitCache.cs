using ARMeilleure;
using ARMeilleure.Memory;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Cpu.LightningJit.Cache
{
    static partial class JitCache
    {
        private static readonly int _pageSize = (int)MemoryBlock.GetPageSize();
        private static readonly int _pageMask = _pageSize - 1;

        private const int CodeAlignment = 4;
        private const int FullCacheSize = 2047 * 1024 * 1024;
        private const int ReducedCacheSize = FullCacheSize / 8;

        private const float EvictionTargetPercentage = 0.20f;
        private const int MaxEntriesToEvictAtOnce = 100;

        // Simple logging configuration
        private const int LogInterval = 5000;  // Log every 5000 allocations

        private static ReservedRegion _jitRegion;
        private static JitCacheInvalidation _jitCacheInvalidator;

        private static CacheMemoryAllocator _cacheAllocator;

        private static readonly List<CacheEntry> _cacheEntries = [];
        private static readonly Dictionary<int, EntryUsageStats> _entryUsageStats = [];

        private static readonly Lock _lock = new();
        private static bool _initialized;
        private static int _cacheSize;

        // Basic statistics
        private static int _totalAllocations = 0;
        private static int _totalEvictions = 0;

        private class EntryUsageStats
        {
            public long LastAccessTime { get; private set; }
            public int UsageCount { get; private set; }

            public EntryUsageStats()
            {
                LastAccessTime = DateTime.UtcNow.Ticks;
                UsageCount = 1;
            }

            public void UpdateUsage()
            {
                LastAccessTime = DateTime.UtcNow.Ticks;
                UsageCount++;
            }
        }

        [SupportedOSPlatform("windows")]
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint FlushInstructionCache(nint hProcess, nint lpAddress, nuint dwSize);

        public static void Initialize(IJitMemoryAllocator allocator)
        {
            if (_initialized)
            {
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                _cacheSize = Optimizations.CacheEviction ? ReducedCacheSize : FullCacheSize;
                _jitRegion = new ReservedRegion(allocator, (ulong)_cacheSize);

                if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
                {
                    _jitCacheInvalidator = new JitCacheInvalidation(allocator);
                }

                _cacheAllocator = new CacheMemoryAllocator(_cacheSize);

                Logger.Info?.Print(LogClass.Cpu, $"Lightning JIT Cache initialized: Size={_cacheSize / (1024 * 1024)} MB, Eviction={Optimizations.CacheEviction}");
                _initialized = true;
            }
        }

        public unsafe static nint Map(ReadOnlySpan<byte> code)
        {
            lock (_lock)
            {
                Debug.Assert(_initialized);
                _totalAllocations++;

                int funcOffset;

                if (Optimizations.CacheEviction)
                {
                    int codeSize = AlignCodeSize(code.Length);
                    funcOffset = _cacheAllocator.Allocate(codeSize);

                    if (funcOffset < 0)
                    {
                        EvictEntries(codeSize);
                        funcOffset = _cacheAllocator.Allocate(codeSize);

                        if (funcOffset < 0)
                        {
                            throw new OutOfMemoryException("JIT Cache exhausted even after eviction.");
                        }
                    }

                    _jitRegion.ExpandIfNeeded((ulong)funcOffset + (ulong)codeSize);
                }
                else
                {
                    funcOffset = Allocate(code.Length);
                }

                nint funcPtr = _jitRegion.Pointer + funcOffset;

                if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    unsafe
                    {
                        fixed (byte* codePtr = code)
                        {
                            JitSupportDarwin.Copy(funcPtr, (nint)codePtr, (ulong)code.Length);
                        }
                    }
                }
                else
                {
                    ReprotectAsWritable(funcOffset, code.Length);
                    code.CopyTo(new Span<byte>((void*)funcPtr, code.Length));
                    ReprotectAsExecutable(funcOffset, code.Length);

                    if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        FlushInstructionCache(Process.GetCurrentProcess().Handle, funcPtr, (nuint)code.Length);
                    }
                    else
                    {
                        _jitCacheInvalidator?.Invalidate(funcPtr, (ulong)code.Length);
                    }
                }

                Add(funcOffset, code.Length);

                // Simple periodic logging
                if (_totalAllocations % LogInterval == 0)
                {
                    LogCacheStatus();
                }

                return funcPtr;
            }
        }

        public static void Unmap(nint pointer)
        {
            lock (_lock)
            {
                Debug.Assert(_initialized);

                int funcOffset = (int)(pointer.ToInt64() - _jitRegion.Pointer.ToInt64());

                if (TryFind(funcOffset, out CacheEntry entry, out int entryIndex) && entry.Offset == funcOffset)
                {
                    _cacheAllocator.Free(funcOffset, AlignCodeSize(entry.Size));
                    _cacheEntries.RemoveAt(entryIndex);

                    if (Optimizations.CacheEviction)
                    {
                        _entryUsageStats.Remove(funcOffset);
                    }
                }
            }
        }

        private static void ReprotectAsWritable(int offset, int size)
        {
            int endOffs = offset + size;

            int regionStart = offset & ~_pageMask;
            int regionEnd = (endOffs + _pageMask) & ~_pageMask;

            _jitRegion.Block.MapAsRwx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private static void ReprotectAsExecutable(int offset, int size)
        {
            int endOffs = offset + size;

            int regionStart = offset & ~_pageMask;
            int regionEnd = (endOffs + _pageMask) & ~_pageMask;

            _jitRegion.Block.MapAsRx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private static int Allocate(int codeSize)
        {
            codeSize = AlignCodeSize(codeSize);

            int allocOffset = _cacheAllocator.Allocate(codeSize);

            if (allocOffset < 0)
            {
                throw new OutOfMemoryException("JIT Cache exhausted.");
            }

            _jitRegion.ExpandIfNeeded((ulong)allocOffset + (ulong)codeSize);

            return allocOffset;
        }

        private static int AlignCodeSize(int codeSize)
        {
            return checked(codeSize + (CodeAlignment - 1)) & ~(CodeAlignment - 1);
        }

        private static void Add(int offset, int size)
        {
            CacheEntry entry = new(offset, size);

            int index = _cacheEntries.BinarySearch(entry);

            if (index < 0)
            {
                index = ~index;
            }

            _cacheEntries.Insert(index, entry);

            if (Optimizations.CacheEviction)
            {
                _entryUsageStats[offset] = new EntryUsageStats();
            }
        }

        public static bool TryFind(int offset, out CacheEntry entry, out int entryIndex)
        {
            lock (_lock)
            {
                int index = _cacheEntries.BinarySearch(new CacheEntry(offset, 0));

                if (index < 0)
                {
                    index = ~index - 1;
                }

                if (index >= 0)
                {
                    entry = _cacheEntries[index];

                    if (Optimizations.CacheEviction && _entryUsageStats.TryGetValue(offset, out var stats))
                    {
                        stats.UpdateUsage();
                    }

                    entryIndex = index;
                    return true;
                }
            }

            entry = default;
            entryIndex = 0;
            return false;
        }

        private static void EvictEntries(int requiredSize)
        {
            if (!Optimizations.CacheEviction)
            {
                return;
            }

            lock (_lock)
            {
                int targetSpace = Math.Max(requiredSize, (int)(_cacheSize * EvictionTargetPercentage));
                int freedSpace = 0;
                int evictedCount = 0;

                var entriesWithStats = _cacheEntries
                    .Where(e => _entryUsageStats.ContainsKey(e.Offset))
                    .Select(e => new {
                        Entry = e,
                        Stats = _entryUsageStats[e.Offset],
                        Score = CalculateEvictionScore(_entryUsageStats[e.Offset])
                    })
                    .OrderBy(x => x.Score)
                    .Take(MaxEntriesToEvictAtOnce)
                    .ToList();

                foreach (var item in entriesWithStats)
                {
                    int entrySize = AlignCodeSize(item.Entry.Size);

                    int entryIndex = _cacheEntries.BinarySearch(item.Entry);
                    if (entryIndex >= 0)
                    {
                        _cacheAllocator.Free(item.Entry.Offset, entrySize);
                        _cacheEntries.RemoveAt(entryIndex);
                        _entryUsageStats.Remove(item.Entry.Offset);

                        freedSpace += entrySize;
                        evictedCount++;

                        if (freedSpace >= targetSpace)
                        {
                            break;
                        }
                    }
                }

                _totalEvictions += evictedCount;

                Logger.Info?.Print(LogClass.Cpu, $"Lightning JIT Cache: Evicted {evictedCount} entries, freed {freedSpace / (1024 * 1024.0):F2} MB");
            }
        }

        private static double CalculateEvictionScore(EntryUsageStats stats)
        {
            long currentTime = DateTime.UtcNow.Ticks;
            long ageInTicks = currentTime - stats.LastAccessTime;

            double ageInSeconds = ageInTicks / 10_000_000.0;

            const double usageWeight = 1.0;
            const double ageWeight = 2.0;

            double usageScore = Math.Log10(stats.UsageCount + 1) * usageWeight;
            double ageScore = (10.0 / (ageInSeconds + 1.0)) * ageWeight;

            return usageScore + ageScore;
        }

        private static void LogCacheStatus()
        {
            int estimatedUsedSize = _cacheEntries.Sum(e => AlignCodeSize(e.Size));
            double usagePercentage = 100.0 * estimatedUsedSize / _cacheSize;

            Logger.Info?.Print(LogClass.Cpu,
                $"Lightning JIT Cache status: entries={_cacheEntries.Count}, " +
                $"est. used={estimatedUsedSize / (1024 * 1024.0):F2} MB ({usagePercentage:F1}%), " +
                $"evictions={_totalEvictions}, allocations={_totalAllocations}");
        }
    }
}
