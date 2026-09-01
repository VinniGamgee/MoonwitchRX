#nullable enable
using System;
using System.Reflection;
using Ryujinx.HLE;

namespace Ryujinx.HLE.Kenjinx
{
    public static class AmiiboBridge
    {
        public static bool TryLoadVirtualAmiibo(Switch device, byte[] data, out string message)
        {
            message = "NFP bridge not wired.";
            if (device?.System is null)
            {
                message = "No System on Switch device.";
                return false;
            }

            // --- TRY 0: Our conflict-free shim class first (recommended) ---
            // Type name including assembly: Ryujinx.HLE
            var shimType = Type.GetType(
                "Ryujinx.HLE.HOS.Services.Nfc.Nfp.KenjinxAmiiboShim, Ryujinx.HLE",
                throwOnError: false
            );
            if (shimType != null)
            {
                var m = shimType.GetMethod(
                    "InjectAmiibo",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(byte[]) },
                    modifiers: null
                );

                if (m != null)
                {
                    var ok = (bool)(m.Invoke(null, new object[] { data }) ?? false);
                    if (ok)
                    {
                        message = "Injected via KenjinxAmiiboShim.InjectAmiibo";
                        return true;
                    }
                }
            }

            // --- TRY 0.5: If available – static API directly on NfpManager ---
            // (Only if you later add a static InjectAmiibo(...) method to NfpManager.)
            var nfpManagerType = Type.GetType(
                "Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager, Ryujinx.HLE",
                throwOnError: false
            );
            if (nfpManagerType != null)
            {
                var m = nfpManagerType.GetMethod(
                    "InjectAmiibo",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(byte[]) },
                    modifiers: null
                );

                if (m != null)
                {
                    var ok = m.Invoke(null, new object[] { data });
                    message = "Injected via NfpManager.InjectAmiibo(static)";
                    return true;
                }
            }

            // --- TRY 1: Direct access to an existing manager in the system object (your existing code) ---
            var sys = device.System;
            var nfpMgr =
                GetField(sys, "NfpManager") ??
                GetField(sys, "_nfpManager") ??
                GetField(sys, "NfcManager") ??
                GetField(sys, "_nfcManager");

            if (nfpMgr != null)
            {
                // Possible method identifiers depending on the tree:
                var mi =
                    GetMethod(nfpMgr, "InjectAmiibo", new[] { typeof(byte[]) }) ??
                    GetMethod(nfpMgr, "LoadAmiiboFromBytes", new[] { typeof(byte[]) }) ??
                    GetMethod(nfpMgr, "LoadVirtualAmiibo", new[] { typeof(byte[]) }) ??
                    GetMethod(nfpMgr, "ScanAmiiboFromBuffer", new[] { typeof(byte[]) });

                if (mi != null)
                {
                    mi.Invoke(nfpMgr, new object[] { data });
                    message = $"Injected via {mi.DeclaringType?.Name}.{mi.Name}";
                    return true;
                }
            }

            message = "No NFP manager API found. Add a fixed InjectAmiibo(byte[]) to your NFP layer (see TODO in AmiiboBridge comments).";
            return false;
        }

        public static void ClearVirtualAmiibo(Switch device)
        {
            if (device?.System is null) return;

            // --- Clear via Shim (if present) ---
            var shimType = Type.GetType(
                "Ryujinx.HLE.HOS.Services.Nfc.Nfp.KenjinxAmiiboShim, Ryujinx.HLE",
                throwOnError: false
            );
            shimType?
                .GetMethod("ClearAmiibo", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?.Invoke(null, null);

            // --- Optional: Static clear API on NfpManager (if available) ---
            var nfpManagerType = Type.GetType(
                "Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager, Ryujinx.HLE",
                throwOnError: false
            );
            nfpManagerType?
                .GetMethod("ClearAmiibo", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?.Invoke(null, null);

            // --- Your existing instance fallback ---
            var sys = device.System;
            var nfpMgr =
                GetField(sys, "NfpManager") ??
                GetField(sys, "_nfpManager") ??
                GetField(sys, "NfcManager") ??
                GetField(sys, "_nfcManager");

            if (nfpMgr != null)
            {
                var mi =
                    GetMethod(nfpMgr, "ClearAmiibo", Type.EmptyTypes) ??
                    GetMethod(nfpMgr, "ResetVirtualAmiibo", Type.EmptyTypes);

                mi?.Invoke(nfpMgr, Array.Empty<object>());
            }
        }

        private static object? GetField(object target, string name)
        {
            var t = target.GetType();
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            return f?.GetValue(target);
        }

        private static MethodInfo? GetMethod(object target, string name, Type[]? sig)
        {
            var t = target.GetType();
            return t.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                binder: null,
                types: sig ?? Type.EmptyTypes,
                modifiers: null);
        }
    }
}
