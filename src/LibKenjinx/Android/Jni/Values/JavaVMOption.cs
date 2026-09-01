using Rxmxnx.PInvoke;
using System;

namespace LibKenjinx.Jni.Values;

public readonly struct JavaVMOption
{
	internal ReadOnlyValPtr<Byte> Name { get; init; }
	internal nint ExtraInfo { get; init; }
}
