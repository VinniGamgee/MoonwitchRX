using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    internal class AutoFlushCounter
    {
        // Desktop values are kept unchanged. On Android tilers we batch a little longer to
        // avoid fragmenting long frames into a large number of tiny queue submissions.
        private readonly long _framebufferFlushTimer;
        private readonly long _drawFlushTimer;
        private readonly int _minDrawCountForFlush;

        // Average wait time that triggers fast flush mode to be entered.
        private readonly static long _fastFlushEnterThreshold = Stopwatch.Frequency / 666; // (1.5ms)

        // Average wait time that triggers fast flush mode to be exited.
        private readonly static long _fastFlushExitThreshold = Stopwatch.Frequency / 10000; // (0.1ms)

        // Number of frames to average waiting times over.
        private const int SyncWaitAverageCount = 20;

        private const int MinConsecutiveQueryForFlush = 10;
        private const int InitialQueryCountForFlush = 32;

        private readonly VulkanRenderer _gd;

        private long _lastFlush;
        private ulong _lastDrawCount;
        private bool _hasPendingQuery;
        private int _consecutiveQueries;
        private int _queryCount;

        private readonly int[] _queryCountHistory = new int[3];
        private int _queryCountHistoryIndex;
        private int _remainingQueries;

        private readonly long[] _syncWaitHistory = new long[SyncWaitAverageCount];
        private int _syncWaitHistoryIndex;

        private bool _fastFlushMode;

        public AutoFlushCounter(VulkanRenderer gd)
        {
            _gd = gd;

            if (PlatformInfo.IsBionic)
            {
                // RX5 Android batching: continue the RX4 direction with moderately larger
                // command batches. Synchronization semantics remain unchanged; only the
                // periodic submission cadence is relaxed for long mobile frames.
                _framebufferFlushTimer = Stopwatch.Frequency / 333; // ~3ms
                _drawFlushTimer = Stopwatch.Frequency / 222; // ~4.5ms
                _minDrawCountForFlush = 28;
            }
            else
            {
                _framebufferFlushTimer = Stopwatch.Frequency / 1000; // 1ms
                _drawFlushTimer = Stopwatch.Frequency / 666; // 1.5ms
                _minDrawCountForFlush = 10;
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

            // Fast flush mode toggle.

            _syncWaitHistory[_syncWaitHistoryIndex] = _gd.SyncManager.GetAndResetWaitTicks();

            _syncWaitHistoryIndex = (_syncWaitHistoryIndex + 1) % SyncWaitAverageCount;

            long averageWait = (long)_syncWaitHistory.Average();

            if (_fastFlushMode ? averageWait < _fastFlushExitThreshold : averageWait > _fastFlushEnterThreshold)
            {
                _fastFlushMode = !_fastFlushMode;
                Logger.Debug?.PrintMsg(LogClass.Gpu, $"Switched fast flush mode: ({_fastFlushMode})");
            }
        }
    }
}
