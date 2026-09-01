using System;
#if ANDROID
using System.Runtime.CompilerServices;
#else
using System.Runtime.InteropServices;
#endif

namespace ARMeilleure.Instructions
{
    static class MathHelper
    {
#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static double Abs(double value)
        {
            return Math.Abs(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static double Ceiling(double value)
        {
            return Math.Ceiling(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static double Floor(double value)
        {
            return Math.Floor(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static double Round(double value, int mode)
        {
            return Math.Round(value, (MidpointRounding)mode);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static double Truncate(double value)
        {
            return Math.Truncate(value);
        }
    }

    static class MathHelperF
    {
#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static float Abs(float value)
        {
            return MathF.Abs(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static float Ceiling(float value)
        {
            return MathF.Ceiling(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static float Floor(float value)
        {
            return MathF.Floor(value);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static float Round(float value, int mode)
        {
            return MathF.Round(value, (MidpointRounding)mode);
        }

#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [UnmanagedCallersOnly]
#endif
        public static float Truncate(float value)
        {
            return MathF.Truncate(value);
        }
    }
}
