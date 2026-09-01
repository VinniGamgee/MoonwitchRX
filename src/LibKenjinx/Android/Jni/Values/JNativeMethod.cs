using Rxmxnx.PInvoke;
using System;

namespace LibKenjinx.Jni.Values;

public readonly struct JNativeMethod
{
	internal ReadOnlyValPtr<Byte> Name { get; init; }
	internal ReadOnlyValPtr<Byte> Signature { get; init; }
	internal nint Pointer { get; init; }
}
