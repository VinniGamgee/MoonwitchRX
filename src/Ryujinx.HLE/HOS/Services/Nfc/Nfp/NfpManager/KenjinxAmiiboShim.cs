#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    /// <summary>
    /// A small static buffer + an API that we access from the Android side via reflection.
    /// </summary>
    public static class KenjinxAmiiboShim
    {
        private static byte[]? s_tag;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InjectAmiibo(byte[] tagBytes)
        {
            if (tagBytes is null || tagBytes.Length == 0) return false;
            s_tag = (byte[])tagBytes.Clone();
            System.Diagnostics.Debug.WriteLine($"[Kenjinx] KenjinxAmiiboShim.InjectAmiibo bytes={tagBytes.Length}");
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ClearAmiibo()
        {
            s_tag = null;
            System.Diagnostics.Debug.WriteLine("[Kenjinx] KenjinxAmiiboShim.ClearAmiibo");
        }

        // ▼ Used by INfp – retrieves the data exactly once a day and empties the buffer
        public static bool TryConsume(out byte[] data)
        {
            if (s_tag is null)
            {
                data = Array.Empty<byte>();
                return false;
            }
            data = s_tag;
            s_tag = null;
            return true;
        }

        // ▼ Convenient alias
        public static void Clear() => ClearAmiibo();

        public static bool HasInjectedAmiibo => s_tag is not null;

        public static ReadOnlySpan<byte> PeekInjectedAmiibo()
            => s_tag is null ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(s_tag);
    }
}
