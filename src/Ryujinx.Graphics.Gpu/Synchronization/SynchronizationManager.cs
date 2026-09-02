using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Device;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Synchronization
{
    /// <summary>
    /// GPU synchronization manager.
    /// </summary>
    public class SynchronizationManager : ISynchronizationManager
    {
        /// <summary>
        /// The maximum number of syncpoints supported by the GM20B.
        /// </summary>
        public const int MaxHardwareSyncpoints = 192;

        /// <summary>
        /// Array containing all hardware syncpoints.
        /// </summary>
        private readonly Syncpoint[] _syncpoints;

        // RX7 rate-limited diagnostics. These counters do not change wait behavior.
        private long _diagWaitCalls;
        private long _diagWaitTicks;
        private long _diagMaxWaitTicks;
        private long _diagTimeouts;
        private long _diagRecoverySleepMs;
        private long _diagLastLogTicks;

        public SynchronizationManager()
        {
            _syncpoints = new Syncpoint[MaxHardwareSyncpoints];
            _diagLastLogTicks = Stopwatch.GetTimestamp();

            for (uint i = 0; i < _syncpoints.Length; i++)
            {
                _syncpoints[i] = new Syncpoint(i);
            }
        }

        private static void UpdateMax(ref long target, long value)
        {
            long current = Volatile.Read(ref target);

            while (value > current)
            {
                long observed = Interlocked.CompareExchange(ref target, value, current);
                if (observed == current)
                {
                    break;
                }

                current = observed;
            }
        }

        private void RecordWait(long elapsedTicks, bool timedOut)
        {
            long calls = Interlocked.Increment(ref _diagWaitCalls);
            Interlocked.Add(ref _diagWaitTicks, elapsedTicks);
            UpdateMax(ref _diagMaxWaitTicks, elapsedTicks);

            if (timedOut)
            {
                Interlocked.Increment(ref _diagTimeouts);
            }

            if ((calls & 31) != 0)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            long previousLog = Volatile.Read(ref _diagLastLogTicks);

            if (now - previousLog < Stopwatch.Frequency * 2 ||
                Interlocked.CompareExchange(ref _diagLastLogTicks, now, previousLog) != previousLog)
            {
                return;
            }

            long windowCalls = Interlocked.Exchange(ref _diagWaitCalls, 0);
            long windowTicks = Interlocked.Exchange(ref _diagWaitTicks, 0);
            long maxTicks = Interlocked.Exchange(ref _diagMaxWaitTicks, 0);
            long timeouts = Interlocked.Exchange(ref _diagTimeouts, 0);
            long recoverySleepMs = Interlocked.Exchange(ref _diagRecoverySleepMs, 0);

            if (windowCalls == 0)
            {
                return;
            }

            double totalMs = windowTicks * 1000.0 / Stopwatch.Frequency;
            double avgMs = totalMs / windowCalls;
            double maxMs = maxTicks * 1000.0 / Stopwatch.Frequency;

            Logger.Info?.PrintMsg(
                LogClass.Gpu,
                $"RX7DIAG SYNCPOINT calls={windowCalls} total={totalMs:F2}ms avg={avgMs:F3}ms max={maxMs:F2}ms timeout={timeouts} recoverySleep={recoverySleepMs}ms");
        }

        /// <inheritdoc/>
        public uint IncrementSyncpoint(uint id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)MaxHardwareSyncpoints);

            return _syncpoints[id].Increment();
        }

        /// <inheritdoc/>
        public uint GetSyncpointValue(uint id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)MaxHardwareSyncpoints);

            return _syncpoints[id].Value;
        }

        /// <summary>
        /// Register a new callback on a syncpoint with a given id at a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <param name="threshold">The target threshold</param>
        /// <param name="callback">The callback to call when the threshold is reached</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHardwareSyncpoints</exception>
        /// <returns>The created SyncpointWaiterHandle object or null if already past threshold</returns>
        public SyncpointWaiterHandle RegisterCallbackOnSyncpoint(uint id, uint threshold, Action<SyncpointWaiterHandle> callback)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)MaxHardwareSyncpoints);

            return _syncpoints[id].RegisterCallback(threshold, callback);
        }

        /// <summary>
        /// Unregister a callback on a given syncpoint.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <param name="waiterInformation">The waiter information to unregister</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHardwareSyncpoints</exception>
        public void UnregisterCallback(uint id, SyncpointWaiterHandle waiterInformation)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)MaxHardwareSyncpoints);

            _syncpoints[id].UnregisterCallback(waiterInformation);
        }

        /// <inheritdoc/>
        public bool WaitOnSyncpoint(uint id, uint threshold, TimeSpan timeout)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, (uint)MaxHardwareSyncpoints);

            // TODO: Remove this when GPU channel scheduling will be implemented.
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            using ManualResetEvent waitEvent = new(false);
            var info = _syncpoints[id].RegisterCallback(threshold, (_) => waitEvent.Set());

            if (info == null)
            {
                return false;
            }

            long beforeTicks = Stopwatch.GetTimestamp();
            bool signaled = waitEvent.WaitOne(timeout);
            long elapsedTicks = Stopwatch.GetTimestamp() - beforeTicks;

            RecordWait(elapsedTicks, !signaled);

            if (!signaled && info != null)
            {
                uint currentValue = _syncpoints[id].Value;
                Logger.Error?.Print(LogClass.Gpu, $"Wait on syncpoint {id} for threshold {threshold} took more than {timeout.TotalMilliseconds}ms (current value: {currentValue}), resuming execution...");

                _syncpoints[id].UnregisterCallback(info);

                // Give the GPU some time to recover if it's struggling.
                Interlocked.Add(ref _diagRecoverySleepMs, 100);
                Thread.Sleep(100);
            }

            return !signaled;
        }
    }
}
