from pathlib import Path

WINDOW = Path("src/Ryujinx.Graphics.Vulkan/Window.cs")
GRADLE = Path("src/KenjinxAndroid/app/build.gradle")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected exactly one match, found {count}")
    return text.replace(old, new, 1)


gradle = GRADLE.read_text(encoding="utf-8")
if "versionName '2.1.0-pr.2-rx8-presentqueue'" in gradle:
    print("RX8 present queue patch already applied")
    raise SystemExit(0)

window = WINDOW.read_text(encoding="utf-8")

window = replace_once(
    window,
    "using Ryujinx.Common;\n",
    "using Ryujinx.Common;\nusing Ryujinx.Common.Logging;\n",
    "logging using",
)

window = replace_once(
    window,
    "using System;\n",
    "using System;\nusing System.Diagnostics;\n",
    "stopwatch using",
)

window = replace_once(
    window,
    "        private int _frameIndex;\n\n        private int _width;",
    "        private int _frameIndex;\n\n"
    "        // RX8 Android present-path telemetry. Kept deliberately lightweight: one summary\n"
    "        // every 128 presented frames, with no per-frame logging.\n"
    "        private int _rx8PresentSamples;\n"
    "        private long _rx8AcquireTicks;\n"
    "        private long _rx8PresentTicks;\n"
    "        private long _rx8MaxAcquireTicks;\n"
    "        private long _rx8MaxPresentTicks;\n"
    "        private bool? _rx8BackgroundPresentQueue;\n\n"
    "        private int _width;",
    "RX8 telemetry fields",
)

window = replace_once(
    window,
    "                var acquireResult = _gd.SwapchainApi.AcquireNextImage(\n"
    "                    _device,\n"
    "                    _swapchain,\n"
    "                    ulong.MaxValue,\n"
    "                    _imageAvailableSemaphores[semaphoreIndex],\n"
    "                    new Fence(),\n"
    "                    ref nextImage);",
    "                long acquireStart = Stopwatch.GetTimestamp();\n"
    "                var acquireResult = _gd.SwapchainApi.AcquireNextImage(\n"
    "                    _device,\n"
    "                    _swapchain,\n"
    "                    ulong.MaxValue,\n"
    "                    _imageAvailableSemaphores[semaphoreIndex],\n"
    "                    new Fence(),\n"
    "                    ref nextImage);\n"
    "                long acquireTicks = Stopwatch.GetTimestamp() - acquireStart;\n"
    "                _rx8AcquireTicks += acquireTicks;\n"
    "                if (acquireTicks > _rx8MaxAcquireTicks)\n"
    "                {\n"
    "                    _rx8MaxAcquireTicks = acquireTicks;\n"
    "                }",
    "AcquireNextImage telemetry",
)

window = replace_once(
    window,
    "            PresentOne(_gd, _renderFinishedSemaphores[semaphoreIndex], _swapchain, nextImage);\n\n"
    "            swapBuffersCallback?.Invoke();",
    "            long presentStart = Stopwatch.GetTimestamp();\n"
    "            bool backgroundPresent = PresentOne(_gd, _renderFinishedSemaphores[semaphoreIndex], _swapchain, nextImage);\n"
    "            long presentTicks = Stopwatch.GetTimestamp() - presentStart;\n\n"
    "            _rx8PresentTicks += presentTicks;\n"
    "            if (presentTicks > _rx8MaxPresentTicks)\n"
    "            {\n"
    "                _rx8MaxPresentTicks = presentTicks;\n"
    "            }\n\n"
    "            _rx8BackgroundPresentQueue ??= backgroundPresent;\n\n"
    "            if (++_rx8PresentSamples >= 128)\n"
    "            {\n"
    "                double tickToMs = 1000.0 / Stopwatch.Frequency;\n"
    "                double acquireTotalMs = _rx8AcquireTicks * tickToMs;\n"
    "                double presentTotalMs = _rx8PresentTicks * tickToMs;\n"
    "                double acquireAvgMs = acquireTotalMs / _rx8PresentSamples;\n"
    "                double presentAvgMs = presentTotalMs / _rx8PresentSamples;\n\n"
    "                Logger.Info?.PrintMsg(\n"
    "                    LogClass.Gpu,\n"
    "                    $\"RX8DIAG PRESENT frames={_rx8PresentSamples} acquireTotal={acquireTotalMs:F2}ms acquireAvg={acquireAvgMs:F3}ms acquireMax={_rx8MaxAcquireTicks * tickToMs:F2}ms presentTotal={presentTotalMs:F2}ms presentAvg={presentAvgMs:F3}ms presentMax={_rx8MaxPresentTicks * tickToMs:F2}ms bgQueue={_rx8BackgroundPresentQueue}\");\n\n"
    "                _rx8PresentSamples = 0;\n"
    "                _rx8AcquireTicks = 0;\n"
    "                _rx8PresentTicks = 0;\n"
    "                _rx8MaxAcquireTicks = 0;\n"
    "                _rx8MaxPresentTicks = 0;\n"
    "            }\n\n"
    "            swapBuffersCallback?.Invoke();",
    "Present telemetry",
)

window = replace_once(
    window,
    "        private static unsafe void PresentOne(\n"
    "            VulkanRenderer gd,\n"
    "            Silk.NET.Vulkan.Semaphore signal,\n"
    "            SwapchainKHR swapchain,\n"
    "            uint imageIndex)",
    "        private static unsafe bool PresentOne(\n"
    "            VulkanRenderer gd,\n"
    "            Silk.NET.Vulkan.Semaphore signal,\n"
    "            SwapchainKHR swapchain,\n"
    "            uint imageIndex)",
    "PresentOne return type",
)

window = replace_once(
    window,
    "            lock (gd.QueueLock)\n"
    "            {\n"
    "                gd.SwapchainApi.QueuePresent(gd.Queue, in presentInfo);\n"
    "            }\n"
    "        }",
    "            // RX8: on Android, use the second queue when the selected present-capable\n"
    "            // queue family exposes one. Present support is a queue-family capability,\n"
    "            // so queue #1 can safely present while queue #0 remains the main submit queue.\n"
    "            // Cross-queue ordering is preserved by the existing render-finished semaphore.\n"
    "            bool useBackgroundQueue = PlatformInfo.IsBionic && gd.BackgroundQueue.Handle != 0;\n"
    "            var presentQueue = useBackgroundQueue ? gd.BackgroundQueue : gd.Queue;\n"
    "            var presentQueueLock = useBackgroundQueue ? gd.BackgroundQueueLock : gd.QueueLock;\n\n"
    "            lock (presentQueueLock)\n"
    "            {\n"
    "                gd.SwapchainApi.QueuePresent(presentQueue, in presentInfo);\n"
    "            }\n\n"
    "            return useBackgroundQueue;\n"
    "        }",
    "background present queue",
)

WINDOW.write_text(window, encoding="utf-8")

gradle = replace_once(
    gradle,
    "versionName '2.1.0-pr.2-rx5-vkbatch2'",
    "versionName '2.1.0-pr.2-rx8-presentqueue'",
    "RX8 version",
)
GRADLE.write_text(gradle, encoding="utf-8")

print("RX8 present queue patch applied successfully")
