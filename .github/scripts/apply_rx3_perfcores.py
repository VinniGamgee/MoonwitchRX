from pathlib import Path

cpp = Path('src/KenjinxAndroid/app/src/main/cpp/kenjinx.cpp')
s = cpp.read_text()
old_includes = '#include <chrono>\n#include <csignal>\n'
new_includes = '''#include <chrono>
#include <csignal>
#include <algorithm>
#include <fstream>
#include <mutex>
#include <sched.h>
#include <string>
#include <unistd.h>
#include <vector>
'''
if old_includes not in s:
    raise SystemExit('kenjinx.cpp include anchor not found')
s = s.replace(old_includes, new_includes, 1)

old_render = '''extern "C"
void setRenderingThread() {
    auto currentId = pthread_self();

    _renderingThreadId = currentId;

    _currentTimePoint = std::chrono::high_resolution_clock::now();
}
'''
new_render = '''namespace {
std::once_flag g_perf_core_init_flag;
cpu_set_t g_perf_core_mask;
bool g_perf_core_mask_valid = false;

long ReadCpuFrequency(const std::string& path) {
    std::ifstream file(path);
    long value = 0;
    if (file.good()) {
        file >> value;
    }
    return value;
}

void InitPerformanceCoreMask() {
    CPU_ZERO(&g_perf_core_mask);

    cpu_set_t allowed;
    CPU_ZERO(&allowed);
    if (sched_getaffinity(0, sizeof(allowed), &allowed) != 0) {
        return;
    }

    struct CoreInfo {
        int id;
        long frequency;
    };

    std::vector<CoreInfo> cores;
    int allowed_count = 0;
    long cpu_count = sysconf(_SC_NPROCESSORS_CONF);
    if (cpu_count <= 0) {
        return;
    }

    cpu_count = std::min<long>(cpu_count, CPU_SETSIZE);
    for (int cpu = 0; cpu < cpu_count; cpu++) {
        if (!CPU_ISSET(cpu, &allowed)) {
            continue;
        }

        allowed_count++;
        const std::string base = "/sys/devices/system/cpu/cpu" + std::to_string(cpu) + "/cpufreq/";
        long frequency = ReadCpuFrequency(base + "cpuinfo_max_freq");
        if (frequency <= 0) {
            frequency = ReadCpuFrequency(base + "scaling_max_freq");
        }

        if (frequency > 0) {
            cores.push_back({cpu, frequency});
        }
    }

    if (allowed_count < 2 || static_cast<int>(cores.size()) != allowed_count) {
        return;
    }

    auto [min_it, max_it] = std::minmax_element(
        cores.begin(), cores.end(),
        [](const CoreInfo& a, const CoreInfo& b) { return a.frequency < b.frequency; });

    const long min_frequency = min_it->frequency;
    const long max_frequency = max_it->frequency;
    if (max_frequency <= min_frequency) {
        return;
    }

    const long threshold = min_frequency + (max_frequency - min_frequency) / 4;
    int selected = 0;
    for (const auto& core : cores) {
        if (core.frequency > threshold) {
            CPU_SET(core.id, &g_perf_core_mask);
            selected++;
        }
    }

    if (selected < 2) {
        CPU_ZERO(&g_perf_core_mask);
        std::sort(cores.begin(), cores.end(),
                  [](const CoreInfo& a, const CoreInfo& b) { return a.frequency > b.frequency; });
        const int target = std::min<int>(static_cast<int>(cores.size()),
                                         std::max<int>(2, static_cast<int>(cores.size()) / 2));
        selected = 0;
        for (int i = 0; i < target; i++) {
            CPU_SET(cores[i].id, &g_perf_core_mask);
            selected++;
        }
    }

    g_perf_core_mask_valid = selected > 0;
}
}

extern "C"
void setPerformanceThread() {
    std::call_once(g_perf_core_init_flag, InitPerformanceCoreMask);
    if (!g_perf_core_mask_valid) {
        return;
    }

    sched_setaffinity(0, sizeof(g_perf_core_mask), &g_perf_core_mask);
}

extern "C"
void setRenderingThread() {
    setPerformanceThread();

    auto currentId = pthread_self();

    _renderingThreadId = currentId;

    _currentTimePoint = std::chrono::high_resolution_clock::now();
}
'''
if old_render not in s:
    raise SystemExit('setRenderingThread anchor not found')
s = s.replace(old_render, new_render, 1)
cpp.write_text(s)

gfx = Path('src/LibKenjinx/LibKenjinx.Graphics.cs')
s = gfx.read_text()
field_anchor = '        private static bool _enableGraphicsLogging;\n'
field_insert = '''        private static bool _enableGraphicsLogging;

        [DllImport("libkenjinxjni", EntryPoint = "setPerformanceThread")]
        private static extern void SetPerformanceThread();
'''
if field_anchor not in s:
    raise SystemExit('LibKenjinx.Graphics field anchor not found')
s = s.replace(field_anchor, field_insert, 1)

run_anchor = '''            var device = SwitchDevice.EmulationContext!;
            _gpuDoneEvent = new ManualResetEvent(true);

            device.Gpu.Renderer.Initialize(_enableGraphicsLogging ? GraphicsDebugLevel.All : GraphicsDebugLevel.None);
'''
run_insert = '''            var device = SwitchDevice.EmulationContext!;
            _gpuDoneEvent = new ManualResetEvent(true);

            if (PlatformInfo.IsBionic)
            {
                SetPerformanceThread();
            }

            device.Gpu.Renderer.Initialize(_enableGraphicsLogging ? GraphicsDebugLevel.All : GraphicsDebugLevel.None);
'''
if run_anchor not in s:
    raise SystemExit('LibKenjinx.Graphics RunLoop anchor not found')
s = s.replace(run_anchor, run_insert, 1)
gfx.write_text(s)

nce = Path('src/Ryujinx.Cpu/Nce/NceCpuContext.cs')
s = nce.read_text()
class_anchor = '''    class NceCpuContext : ICpuContext
    {
'''
class_insert = '''    class NceCpuContext : ICpuContext
    {
        [DllImport("libkenjinxjni", EntryPoint = "setPerformanceThread")]
        private static extern void SetPerformanceThread();

'''
if class_anchor not in s:
    raise SystemExit('NceCpuContext class anchor not found')
s = s.replace(class_anchor, class_insert, 1)

exec_anchor = '''        public void Execute(IExecutionContext context, ulong address)
        {
            NceExecutionContext nec = (NceExecutionContext)context;
'''
exec_insert = '''        public void Execute(IExecutionContext context, ulong address)
        {
            if (OperatingSystem.IsAndroid())
            {
                SetPerformanceThread();
            }

            NceExecutionContext nec = (NceExecutionContext)context;
'''
if exec_anchor not in s:
    raise SystemExit('NceCpuContext Execute anchor not found')
s = s.replace(exec_anchor, exec_insert, 1)
nce.write_text(s)

gradle = Path('src/KenjinxAndroid/app/build.gradle')
s = gradle.read_text()
if "versionName '2.1.0-pr.2-rx2-gal'" not in s:
    raise SystemExit('RX2 version string not found')
s = s.replace("versionName '2.1.0-pr.2-rx2-gal'", "versionName '2.1.0-pr.2-rx3-perfcores'", 1)
gradle.write_text(s)
