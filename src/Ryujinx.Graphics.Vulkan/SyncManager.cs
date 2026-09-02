using Ryujinx.Common.Logging;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan
{
    class SyncManager
    {
        private class SyncHandle
        {
            public ulong ID;
            public MultiFenceHolder Waitable;
            public ulong FlushId;
            public bool Signalled;

            public bool NeedsFlush(ulong currentFlushId)
            {
                return (long)(FlushId - currentFlushId) >= 0;
            }
        }

        private ulong _firstHandle;

        private readonly VulkanRenderer _gd;
        private readonly Device _device;
        private readonly List<SyncHandle> _handles;
        private ulong _flushId;
        private long _waitTicks;

        // RX7 diagnostics are deliberately separate from _waitTicks because AutoFlushCounter
        // consumes and resets that value every present. These counters are only used for a
        // rate-limited summary and do not change synchronization behavior.
        private long _diagWaitCalls;
        private long _diagWaitTicks;
        private long _diagMaxWaitTicks;
        private long _diagForcedFlushes;
        private long _diagTimeouts;
        private long _diagLastLogTicks;

        public SyncManager(VulkanRenderer gd, Device device)
        {
            _gd = gd;
            _device = device;
            _handles = [];
            _diagLastLogTicks = Stopwatch.GetTimestamp();
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

            // Avoid checking/logging on every hot-path wait. At most one check per 64 waits,
            // and summaries are further limited to roughly once every 2 seconds.
            if ((calls & 63) != 0)
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
            long forcedFlushes = Interlocked.Exchange(ref _diagForcedFlushes, 0);
            long timeouts = Interlocked.Exchange(ref _diagTimeouts, 0);

            if (windowCalls == 0)
            {
                return;
            }

            double totalMs = windowTicks * 1000.0 / Stopwatch.Frequency;
            double avgMs = totalMs / windowCalls;
            double maxMs = maxTicks * 1000.0 / Stopwatch.Frequency;

            Logger.Info?.PrintMsg(
                LogClass.Gpu,
                $"RX7DIAG VKWAIT calls={windowCalls} total={totalMs:F2}ms avg={avgMs:F3}ms max={maxMs:F2}ms forcedFlush={forcedFlushes} timeout={timeouts}");
        }

        public void RegisterFlush()
        {
            _flushId++;
        }

        public void Create(ulong id, bool strict)
        {
            ulong flushId = _flushId;
            MultiFenceHolder waitable = new();
            if (strict || _gd.InterruptAction == null)
            {
                _gd.FlushAllCommands();
                _gd.CommandBufferPool.AddWaitable(waitable);
            }
            else
            {
                // Don't flush commands, instead wait for the current command buffer to finish.
                // If this sync is waited on before the command buffer is submitted, interrupt the gpu thread and flush it manually.

                _gd.CommandBufferPool.AddInUseWaitable(waitable);
            }

            SyncHandle handle = new()
            {
                ID = id,
                Waitable = waitable,
                FlushId = flushId,
            };

            lock (_handles)
            {
                _handles.Add(handle);
            }
        }

        public ulong GetCurrent()
        {
            lock (_handles)
            {
                ulong lastHandle = _firstHandle;

                foreach (SyncHandle handle in _handles)
                {
                    lock (handle)
                    {
                        if (handle.Waitable == null)
                        {
                            continue;
                        }

                        if (handle.ID > lastHandle)
                        {
                            bool signaled = handle.Signalled || handle.Waitable.WaitForFences(_gd.Api, _device, 0);
                            if (signaled)
                            {
                                lastHandle = handle.ID;
                                handle.Signalled = true;
                            }
                        }
                    }
                }

                return lastHandle;
            }
        }

        public void Wait(ulong id)
        {
            SyncHandle result = null;

            lock (_handles)
            {
                if ((long)(_firstHandle - id) > 0)
                {
                    return; // The handle has already been signalled or deleted.
                }

                foreach (SyncHandle handle in _handles)
                {
                    if (handle.ID == id)
                    {
                        result = handle;
                        break;
                    }
                }
            }

            if (result != null)
            {
                if (result.Waitable == null)
                {
                    return;
                }

                long beforeTicks = Stopwatch.GetTimestamp();

                if (result.NeedsFlush(_flushId))
                {
                    _gd.InterruptAction(() =>
                    {
                        if (result.NeedsFlush(_flushId))
                        {
                            Interlocked.Increment(ref _diagForcedFlushes);
                            _gd.FlushAllCommands();
                        }
                    });
                }

                lock (result)
                {
                    if (result.Waitable == null)
                    {
                        return;
                    }

                    bool alreadySignalled = result.Signalled;
                    bool signaled = alreadySignalled || result.Waitable.WaitForFences(_gd.Api, _device, 1000000000);
                    long elapsedTicks = Stopwatch.GetTimestamp() - beforeTicks;

                    if (!alreadySignalled)
                    {
                        RecordWait(elapsedTicks, !signaled);
                    }

                    if (!signaled)
                    {
                        Logger.Error?.PrintMsg(LogClass.Gpu, $"VK Sync Object {result.ID} failed to signal within 1000ms. Continuing...");
                    }
                    else
                    {
                        _waitTicks += elapsedTicks;
                        result.Signalled = true;
                    }
                }
            }
        }

        public void Cleanup()
        {
            // Iterate through handles and remove any that have already been signalled.

            while (true)
            {
                SyncHandle first = null;
                lock (_handles)
                {
                    first = _handles.FirstOrDefault();
                }

                if (first == null || first.NeedsFlush(_flushId))
                {
                    break;
                }

                bool signaled = first.Waitable.WaitForFences(_gd.Api, _device, 0);
                if (signaled)
                {
                    // Delete the sync object.
                    lock (_handles)
                    {
                        lock (first)
                        {
                            _firstHandle = first.ID + 1;
                            _handles.RemoveAt(0);
                            Array.Clear(first.Waitable.Fences);
                            MultiFenceHolder.FencePool.Release(first.Waitable.Fences);
                            first.Waitable = null;
                        }
                    }
                }
                else
                {
                    // This sync handle and any following have not been reached yet.
                    break;
                }
            }
        }

        public long GetAndResetWaitTicks()
        {
            long result = _waitTicks;
            _waitTicks = 0;

            return result;
        }
    }
}
