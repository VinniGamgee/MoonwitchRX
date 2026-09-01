using System;
using System.Diagnostics.CodeAnalysis;

namespace LibKenjinx.Jni.Values
{
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "This struct is created only by binary operations.")]
    public readonly struct JInvokeInterface
    {
#pragma warning disable 0169
        private readonly nint _reserved0;
        private readonly nint _reserved1;
        private readonly nint _reserved2;
#pragma warning restore 0169
        internal nint DestroyJavaVMPointer { get; init; }
        internal nint AttachCurrentThreadPointer { get; init; }
        internal nint DetachCurrentThreadPointer { get; init; }
        internal nint GetEnvPointer { get; init; }
        internal nint AttachCurrentThreadAsDaemonPointer { get; init; }
    }
}
