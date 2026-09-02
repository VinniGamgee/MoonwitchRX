using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    internal class AutoFlushCounter
    {
        // Desktop values are kept unchanged. Android uses an adaptive cadence that can
        // batch substantially longer while synchronization pressure stays low.
        private long _framebufferFlushTimer;
        private long _drawFlushTimer;
        private int _minDrawCountForFlush;

        // Average wait time that triggers fast flush mode to be entered.
        private readonly static long _fastFlushEnterThreshold = Stopwatch.Frequency / 666; // 1.5ms

        // Average wait time that triggers fast flush mode to be exited.
        private readonly static long _fastFlushExitThreshold = Stopwatch.Frequency / 10000; // 0.1ms

        // RX6 Android adaptive thresholds. These only select the periodic submission cadence;
        // actual synchronization/fence semantics are unchanged.
        private readonly static long _androidBalancedSyncThreshold = Stopwatch.Frequency / 333; // ~3ms
        private readonly static long _androidGuardedSyncThreshold = Stopwatch.Frequency / 125; // 8ms

        // Number of frames to average waiting times over.
        private const int SyncWaitAverageCount = 20;
        private const int MinSamplesForAdaptiveBatching = 8;

        private const int MinConsecutiveQueryForFlush = 10;
        private const int InitialQueryCountForFlush = 32;

        private readonly VulkanRenderer _gd;

        private long _lastFlush;
        private ulong _lastDrawCount;
        private bool _hasPendingQuery;
        private int _consecutiveQueries;
        private int _queryCount;

        private readonly long[] _syncWaitHistory = new long[SyncWaitAverageCount];
        private int _syncWaitHistoryIndex;
        private int _syncWaitSamples;

        private readonly int[] _queryCountHistory = new int[3];
        private int _queryCountHistoryIndex;
        private int _remainingQueries;

        private int _androidBatchTier = 1;
        private bool _fastFlushMode;

        public AutoFlushCounter(VulkanRenderer gd)
        {
            _gd = gd;

            if (PlatformInfo.IsBionic)
            {
                // Start from the proven RX5 cadence. RX6 will move to a larger or smaller
                // batch tier after a short warm-up once real synchronization pressure exists.
                ApplyAndroidBatchTier(1, false);
            }
            else
            {
                _framebufferFlushTimer = Stopwatch.Frequency / 1000; // 1ms
                _drawFlushTimer = Stopwatch.Frequency / 666; // 1.5ms
                _minDrawCountForFlush = 10;
            }
        }

        private void ApplyAndroidBatchTier(int tier, bool logChange = true)
        {
            _androidBatchTier = tier;

            switch (tier)
            {
                case 0:
                    // Guarded: RX4-like cadence for genuinely sync-heavy moments.
                    _framebufferFlushTimer = Stopwatch.Frequency / 500; // 2ms
                    _drawFlushTimer = Stopwatch.Frequency / 333; // ~3ms
                    _minDrawCountForFlush = 20;
                    break;
                case 1:
                    // Balanced: RX5 cadence.
                    _framebufferFlushTimer = Stopwatch.Frequency / 333; // ~3ms
                    _drawFlushTimer = Stopwatch.Frequency / 222; // ~4.5ms
                    _minDrawCountForFlush = 28;
                    break;
                default:
                    // Aggressive: larger batches for mobile frames when waits are under control.
                    _framebufferFlushTimer = Stopwatch.Frequency / 250; // 4ms
                    _drawFlushTimer = Stopwatch.Frequency / 154; // ~6.5ms
                    _minDrawCountForFlush = 40;
                    break;
            }

            if (logChange)
            {
                Logger.Debug?.PrintMsg(LogClass.Gpu, $"RX6 Android Vulkan batch tier: {_androidBatchTier}");
            }
        }

        private void UpdateAndroidBatchTier(long averageWait)
        {
            if (!PlatformInfo.IsBionic || _syncWaitSamples < MinSamplesForAdaptiveBatching)
            {
                return;
            }

            int nextTier;

            if (averageWait > _androidGuardedSyncThreshold)
            {
                nextTier = 0;
            }
            else if (averageWait > _androidBalancedSyncThreshold)
            {
                nextTier = 1;
            }
            else
            {
                nextTier = 2;
            }

            if (nextTier != _androidBatchTier)
            {
                ApplyAndroidBatchTier(nextTier);
            }
        }

        public void RegisterFlush(ulong drawCount)
        {
            _lastFlush = Stopwatch.GetTimestamp();
            _lastDrawCount = drawCount;

            _hasPendingQuery = false;
            _consecutiveQueries = 0;
        }

        public bool RegisterPendingQuery()
        {
            _hasPendingQuery = true;
            _consecutiveQueries++;
            _remainingQueries--;

            _queryCountHistory[_queryCountHistoryIndex]++;

            // Interrupt render passes to flush queries, so that early results arrive sooner.
            if (++_queryCount == InitialQueryCountForFlush)
            {
                return true;
            }

            return false;
        }

        public int GetRemainingQueries()
        {
            if (_remainingQueries <= 0)
            {
                _remainingQueries = 16;
            }

            if (_queryCount < InitialQueryCountForFlush)
            {
                return Math.Min(InitialQueryCountForFlush - _queryCount, _remainingQueries);
            }

            return _remainingQueries;
        }

        public bool ShouldFlushQuery()
        {
            return _hasPendingQuery;
        }

        public bool ShouldFlushDraw(ulong drawCount)
        {
            if (_fastFlushMode)
            {
                long draws = (long)(drawCount - _lastDrawCount);

                if (draws < _minDrawCountForFlush)
                {
                    if (draws == 0)
                    {
                        _lastFlush = Stopwatch.GetTimestamp();
                    }

                    return false;
                }

                long now = Stopwatch.GetTimestamp();

                return now > _lastFlush + _drawFlushTimer;
            }

            return false;
        }

        public bool ShouldFlushAttachmentChange(ulong drawCount)
        {
            _queryCount = 0;

            // Flush when there's an attachment change out of a large block of queries.
            if (_consecutiveQueries > MinConsecutiveQueryForFlush)
            {
                return true;
            }

            _consecutiveQueries = 0;

            long draws = (long)(drawCount - _lastDrawCount);

            if (draws < _minDrawCountForFlush)
            {
                if (draws == 0)
                {
                    _lastFlush = Stopwatch.GetTimestamp();
                }

                return false;
            }

            long now = Stopwatch.GetTimestamp();

            return now > _lastFlush + _framebufferFlushTimer;
        }

        public void Present()
        {
            // Query flush prediction.

            _queryCountHistoryIndex = (_queryCountHistoryIndex + 1) % 3;

            _remainingQueries = _queryCountHistory.Max() + 10;

            _queryCountHistory[_queryCountHistoryIndex] = 0;

            // Fast flush mode and RX6 adaptive Android batching.

            _syncWaitHistory[_syncWaitHistoryIndex] = _gd.SyncManager.GetAndResetWaitTicks();

            _syncWaitHistoryIndex = (_syncWaitHistoryIndex + 1) % SyncWaitAverageCount;
            _syncWaitSamples = Math.Min(_syncWaitSamples + 1, SyncWaitAverageCount);

            long averageWait = (long)_syncWaitHistory.Take(_syncWaitSamples).Average();

            UpdateAndroidBatchTier(averageWait);

            if (_fastFlushMode ? averageWait < _fastFlushExitThreshold : averageWait > _fastFlushEnterThreshold)
            {
                _fastFlushMode = !_fastFlushMode;
                Logger.Debug?.PrintMsg(LogClass.Gpu, $"Switched fast flush mode: ({_fastFlushMode})");
            }
        }
    }
}
