// Write C++ code here.
//
// Do not forget to dynamically load the C++ library into your application.
//
// For instance,
//
// In MainActivity.java:
//    static {
//       System.loadLibrary("kenjinxjni");
//    }
//
// Or, in MainActivity.kt:
//    companion object {
//      init {
//         System.loadLibrary("kenjinxjni")
//      }
//    }

#include "kenjinx.h"
#include "pthread.h"
#include <chrono>
#include <csignal>
#include <algorithm>
#include <fstream>
#include <mutex>
#include <sched.h>
#include <string>
#include <unistd.h>
#include <vector>


std::chrono::time_point<std::chrono::steady_clock, std::chrono::nanoseconds> _currentTimePoint;

extern "C"
{
JNIEXPORT jlong JNICALL
Java_org_kenjinx_android_NativeHelpers_getNativeWindow(
        JNIEnv *env,
        jobject instance,
        jobject surface) {
    auto nativeWindow = ANativeWindow_fromSurface(env, surface);
    return nativeWindow == nullptr ? -1 : (jlong) nativeWindow;
}

JNIEXPORT void JNICALL
Java_org_kenjinx_android_NativeHelpers_releaseNativeWindow(
        JNIEnv *env,
        jobject instance,
        jlong window) {
    auto nativeWindow = (ANativeWindow *) window;

    if (nativeWindow != nullptr)
        ANativeWindow_release(nativeWindow);
}

long createSurface(long native_surface, long instance) {
    auto nativeWindow = (ANativeWindow *) native_surface;
    VkSurfaceKHR surface;
    auto vkInstance = (VkInstance) instance;
    auto fpCreateAndroidSurfaceKHR =
            reinterpret_cast<PFN_vkCreateAndroidSurfaceKHR>(vkGetInstanceProcAddr(vkInstance,
                                                                                  "vkCreateAndroidSurfaceKHR"));
    if (fpCreateAndroidSurfaceKHR == nullptr)
        LOGE("Could not get function pointer to CreateAndroidSurfaceKHR");

    VkAndroidSurfaceCreateInfoKHR info;
    info.sType = VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR;
    info.pNext = nullptr;
    info.flags = 0;
    info.window = nativeWindow;
    VK_CHECK(fpCreateAndroidSurfaceKHR(vkInstance, &info, nullptr, &surface));
    return (long) surface;
}

JNIEXPORT jlong JNICALL
Java_org_kenjinx_android_NativeHelpers_getCreateSurfacePtr(
        JNIEnv *env,
        jobject instance) {
    return (jlong) createSurface;
}

char *getStringPointer(
        JNIEnv *env,
        jstring jS) {
    const char *cparam = env->GetStringUTFChars(jS, nullptr);
    auto len = env->GetStringUTFLength(jS);
    char *s = new char[len + 1]; //null terminator
    strcpy(s, cparam);
    env->ReleaseStringUTFChars(jS, cparam);

    return s;
}

jstring createString(
        JNIEnv *env,
        char *ch) {
    auto str = env->NewStringUTF(ch);

    return str;
}

jstring createStringFromStdString(
        JNIEnv *env,
        std::string s) {
    auto str = env->NewStringUTF(s.c_str());

    return str;
}


}
namespace {
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
extern "C"
JNIEXPORT void JNICALL
Java_org_kenjinx_android_MainActivity_initVm(JNIEnv *env, jobject thiz) {
    JavaVM *vm = nullptr;
    env->GetJavaVM(&vm);
    _vm = vm;
    _mainActivity = thiz;
    _mainActivityClass = env->GetObjectClass(thiz);
}

bool isInitialOrientationFlipped = true;

extern "C"
void setCurrentTransform(long native_window, int transform) {
    if (native_window == 0 || native_window == -1)
        return;
    auto nativeWindow = (ANativeWindow *) native_window;

    auto nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;

    transform = transform >> 1;

    // transform is a valid VkSurfaceTransformFlagBitsKHR
    switch (transform) {
        case 0x1:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;
            break;
        case 0x2:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_90;
            break;
        case 0x4:
            nativeTransform = isInitialOrientationFlipped
                              ? ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY
                              : ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_180;
            break;
        case 0x8:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_ROTATE_270;
            break;
        case 0x10:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_HORIZONTAL;
            break;
        case 0x20:
            nativeTransform = static_cast<ANativeWindowTransform>(
                    ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_HORIZONTAL |
                    ANATIVEWINDOW_TRANSFORM_ROTATE_90);
            break;
        case 0x40:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_VERTICAL;
            break;
        case 0x80:
            nativeTransform = static_cast<ANativeWindowTransform>(
                    ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_MIRROR_VERTICAL |
                    ANATIVEWINDOW_TRANSFORM_ROTATE_90);
            break;
        case 0x100:
            nativeTransform = ANativeWindowTransform::ANATIVEWINDOW_TRANSFORM_IDENTITY;
            break;
    }

    nativeWindow->perform(nativeWindow, NATIVE_WINDOW_SET_BUFFERS_TRANSFORM,
                          static_cast<int32_t>(nativeTransform));
}

extern "C"
JNIEXPORT jlong JNICALL
Java_org_kenjinx_android_NativeHelpers_loadDriver(JNIEnv *env, jobject thiz,
                                                  jstring native_lib_path,
                                                  jstring private_apps_path,
                                                  jstring driver_name) {
    auto libPath = getStringPointer(env, native_lib_path);
    auto privateAppsPath = getStringPointer(env, private_apps_path);
    auto driverName = getStringPointer(env, driver_name);

    auto handle = adrenotools_open_libvulkan(
            RTLD_NOW,
            ADRENOTOOLS_DRIVER_CUSTOM,
            nullptr,
            libPath,
            privateAppsPath,
            driverName,
            nullptr,
            nullptr
    );

    delete libPath;
    delete privateAppsPath;
    delete driverName;

    return (jlong) handle;
}

extern "C"
void debug_break(int code) {
    if (code >= 3)
        int r = 0;
}

extern "C"
JNIEXPORT void JNICALL
Java_org_kenjinx_android_NativeHelpers_setTurboMode(JNIEnv *env, jobject thiz, jboolean enable) {
    adrenotools_set_turbo(enable);
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_kenjinx_android_NativeHelpers_getMaxSwapInterval(JNIEnv *env, jobject thiz,
                                                          jlong native_window) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->maxSwapInterval;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_kenjinx_android_NativeHelpers_getMinSwapInterval(JNIEnv *env, jobject thiz,
                                                          jlong native_window) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->minSwapInterval;
}

extern "C"
JNIEXPORT jint JNICALL
Java_org_kenjinx_android_NativeHelpers_setSwapInterval(JNIEnv *env, jobject thiz,
                                                       jlong native_window, jint swap_interval) {
    auto nativeWindow = (ANativeWindow *) native_window;

    return nativeWindow->setSwapInterval(nativeWindow, swap_interval);
}

extern "C"
JNIEXPORT jstring JNICALL
Java_org_kenjinx_android_NativeHelpers_getStringJava(JNIEnv *env, jobject thiz, jlong ptr) {
    return createString(env, (char*)ptr);
}

extern "C"
JNIEXPORT void JNICALL
Java_org_kenjinx_android_NativeHelpers_setIsInitialOrientationFlipped(JNIEnv *env, jobject thiz,
                                                                      jboolean is_flipped) {
    isInitialOrientationFlipped = is_flipped;
}
