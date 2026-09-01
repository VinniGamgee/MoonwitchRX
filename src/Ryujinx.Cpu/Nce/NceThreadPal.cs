using Ryujinx.Common;
using System;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadPal
    {
        private const int SigUsr2Linux = 12;
        private const int SigUsr2MacOS = 31;

        public static int UnixSuspendSignal => OperatingSystem.IsMacOS() ? SigUsr2MacOS : SigUsr2Linux;

        public static nint GetCurrentThreadHandle()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || PlatformInfo.IsBionic)
            {
                return NceThreadPalUnix.GetCurrentThreadHandle();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void SuspendThread(nint handle)
        {
            if (PlatformInfo.IsBionic)
            {
                NceThreadPalAndroid.SuspendThread(handle);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                NceThreadPalUnix.SuspendThread(handle);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
