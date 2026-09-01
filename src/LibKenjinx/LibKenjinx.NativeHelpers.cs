using Ryujinx.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibKenjinx
{
    public static partial class LibKenjinx
    {
        private unsafe static nint CreateStringArray(List<string> strings)
        {
            uint size = (uint)(Marshal.SizeOf<nint>() * (strings.Count + 1));
            var array = (char**)Marshal.AllocHGlobal((int)size);
            Unsafe.InitBlockUnaligned(array, 0, size);

            for (int i = 0; i < strings.Count; i++)
            {
                array[i] = (char*)Marshal.StringToHGlobalAnsi(strings[i]);
            }

            return (nint)array;
        }

        private static void ApplyFullscreenStretch(bool enable)
        {
            var ar = enable ? AspectRatio.Stretched : AspectRatio.Fixed16x9;

            var cfg = GraphicsConfiguration;
            cfg.AspectRatio = ar;
            GraphicsConfiguration = cfg;

            var dev = SwitchDevice?.EmulationContext;
            if (dev != null)
            {
                try { dev.Configuration.AspectRatio = ar; } catch { }
            }
        }
    }
}
