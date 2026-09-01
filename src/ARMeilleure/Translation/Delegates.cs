using ARMeilleure.Instructions;
using System;
using System.Collections.Generic;
using System.Reflection;
#if ANDROID
using ARMeilleure.State;
using System.Runtime.InteropServices;
#endif

namespace ARMeilleure.Translation
{
    static class Delegates
    {
#if ANDROID
        // MathHelper delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double MathHelperAbsDelegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double MathHelperCeilingDelegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double MathHelperFloorDelegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double MathHelperRoundDelegate(double value, int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double MathHelperTruncateDelegate(double value);

        // MathHelperF delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float MathHelperFAbsDelegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float MathHelperFCeilingDelegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float MathHelperFFloorDelegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float MathHelperFRoundDelegate(float value, int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float MathHelperFTruncateDelegate(float value);

        // NativeInterface delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceBreakDelegate(ulong address, int imm);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate byte NativeInterfaceCheckSynchronizationDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceEnqueueForRejitDelegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetCntfrqEl0Delegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetCntpctEl0Delegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetCntvctEl0Delegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetCtrEl0Delegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetDczidEl0Delegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceGetFunctionAddressDelegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceInvalidateCacheLineDelegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate byte NativeInterfaceReadByteDelegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ushort NativeInterfaceReadUInt16Delegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint NativeInterfaceReadUInt32Delegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong NativeInterfaceReadUInt64Delegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 NativeInterfaceReadVector128Delegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceSignalMemoryTrackingDelegate(ulong address, ulong size, byte write);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceSupervisorCallDelegate(ulong address, int imm);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceThrowInvalidMemoryAccessDelegate(ulong address);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceUndefinedDelegate(ulong address, int opCode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceWriteByteDelegate(ulong address, byte value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceWriteUInt16Delegate(ulong address, ushort value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceWriteUInt32Delegate(ulong address, uint value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceWriteUInt64Delegate(ulong address, ulong value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeInterfaceWriteVector128Delegate(ulong address, V128 value);

        // SoftFallback delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong SoftFallbackCountLeadingSignsDelegate(ulong value, int size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong SoftFallbackCountLeadingZerosDelegate(ulong value, int size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32bDelegate(uint crc, byte val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32cbDelegate(uint crc, byte val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32chDelegate(uint crc, ushort val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32cwDelegate(uint crc, uint val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32cxDelegate(uint crc, ulong val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32hDelegate(uint crc, ushort val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32wDelegate(uint crc, uint val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackCrc32xDelegate(uint crc, ulong val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackDecryptDelegate(V128 value, V128 roundKey);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackEncryptDelegate(V128 value, V128 roundKey);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackFixedRotateDelegate(uint hash_e);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackHashChooseDelegate(V128 hash_abcd, uint hash_e, V128 wk);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackHashLowerDelegate(V128 hash_abcd, V128 hash_efgh, V128 wk);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackHashMajorityDelegate(V128 hash_abcd, uint hash_e, V128 wk);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackHashParityDelegate(V128 hash_abcd, uint hash_e, V128 wk);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackHashUpperDelegate(V128 hash_abcd, V128 hash_efgh, V128 wk);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackInverseMixColumnsDelegate(V128 value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackMixColumnsDelegate(V128 value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackPolynomialMult64_128Delegate(ulong op1, ulong op2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SoftFallbackSatF32ToS32Delegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long SoftFallbackSatF32ToS64Delegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackSatF32ToU32Delegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong SoftFallbackSatF32ToU64Delegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SoftFallbackSatF64ToS32Delegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long SoftFallbackSatF64ToS64Delegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint SoftFallbackSatF64ToU32Delegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong SoftFallbackSatF64ToU64Delegate(double value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackSha1SchedulePart1Delegate(V128 w0_3, V128 w4_7, V128 w8_11);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackSha1SchedulePart2Delegate(V128 tw0_3, V128 w12_15);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackSha256SchedulePart1Delegate(V128 w0_3, V128 w4_7);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackSha256SchedulePart2Delegate(V128 w0_3, V128 w8_11, V128 w12_15);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long SoftFallbackSignedShrImm64Delegate(long value, long roundConst, int shift);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbl1Delegate(V128 vector, int bytes, V128 table);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbl2Delegate(V128 vector, int bytes, V128 table0, V128 table1);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbl3Delegate(V128 vector, int bytes, V128 table0, V128 table1, V128 table2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbl4Delegate(V128 vector, int bytes, V128 table0, V128 table1, V128 table2, V128 table3);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbx1Delegate(V128 dest, V128 vector, int bytes, V128 table);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbx2Delegate(V128 dest, V128 vector, int bytes, V128 table0, V128 table1);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbx3Delegate(V128 dest, V128 vector, int bytes, V128 table0, V128 table1, V128 table2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate V128 SoftFallbackTbx4Delegate(V128 dest, V128 vector, int bytes, V128 table0, V128 table1, V128 table2, V128 table3);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ulong SoftFallbackUnsignedShrImm64Delegate(ulong value, long roundConst, int shift);

        // SoftFloat delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat16_32FPConvertDelegate(ushort value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat16_64FPConvertDelegate(ushort value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPAddDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPAddFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SoftFloat32FPCompareDelegate(float a, float b, byte signalNaNs);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareEQDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareEQFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareGEDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareGEFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareGTDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareGTFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareLEDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareLEFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareLTDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPCompareLTFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPDivDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMaxDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMaxFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMaxNumDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMaxNumFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMinDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMinFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMinNumDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMinNumFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulFpscrDelegate(float a, float b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulAddDelegate(float a, float b, float c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulAddFpscrDelegate(float a, float b, float c, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulSubDelegate(float a, float b, float c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulSubFpscrDelegate(float a, float b, float c, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPMulXDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPNegMulAddDelegate(float a, float b, float c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPNegMulSubDelegate(float a, float b, float c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRecipEstimateDelegate(float a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRecipEstimateFpscrDelegate(float a, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRecipStepDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRecipStepFusedDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRecpXDelegate(float a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRSqrtEstimateDelegate(float a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRSqrtEstimateFpscrDelegate(float a, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRSqrtStepDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPRSqrtStepFusedDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPSqrtDelegate(float a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float SoftFloat32FPSubDelegate(float a, float b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ushort SoftFloat32_16FPConvertDelegate(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPAddDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPAddFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SoftFloat64FPCompareDelegate(double a, double b, byte signalNaNs);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareEQDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareEQFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareGEDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareGEFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareGTDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareGTFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareLEDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareLEFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareLTDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPCompareLTFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPDivDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMaxDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMaxFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMaxNumDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMaxNumFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMinDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMinFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMinNumDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMinNumFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulFpscrDelegate(double a, double b, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulAddDelegate(double a, double b, double c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulAddFpscrDelegate(double a, double b, double c, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulSubDelegate(double a, double b, double c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulSubFpscrDelegate(double a, double b, double c, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPMulXDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPNegMulAddDelegate(double a, double b, double c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPNegMulSubDelegate(double a, double b, double c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRecipEstimateDelegate(double a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRecipEstimateFpscrDelegate(double a, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRecipStepDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRecipStepFusedDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRecpXDelegate(double a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRSqrtEstimateDelegate(double a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRSqrtEstimateFpscrDelegate(double a, byte standardFpscr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRSqrtStepDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPRSqrtStepFusedDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPSqrtDelegate(double a);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double SoftFloat64FPSubDelegate(double a, double b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ushort SoftFloat64_16FPConvertDelegate(double value);

        private static readonly Dictionary<string, Delegate> _androidDelegates;
#endif
        public static bool TryGetDelegateFuncPtrByIndex(int index, out nint funcPtr)
        {
            if (index >= 0 && index < _delegates.Count)
            {
                funcPtr = _delegates.Values[index].FuncPtr; // O(1).

                return true;
            }
            else
            {
                funcPtr = default;

                return false;
            }
        }

        public static nint GetDelegateFuncPtrByIndex(int index)
        {
            if (index < 0 || index >= _delegates.Count)
            {
                throw new ArgumentOutOfRangeException($"({nameof(index)} = {index})");
            }

            return _delegates.Values[index].FuncPtr; // O(1).
        }

        public static int GetDelegateIndex(MethodInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);

            string key = GetKey(info);

            int index = _delegates.IndexOfKey(key); // O(log(n)).

            if (index == -1)
            {
                throw new KeyNotFoundException($"({nameof(key)} = {key})");
            }

            return index;
        }

        private static void SetDelegateInfo(MethodInfo method)
        {
            string key = GetKey(method);

#if ANDROID
            if (_androidDelegates.TryGetValue(key, out Delegate del))
            {
                nint funcPtr = Marshal.GetFunctionPointerForDelegate(del);
                _delegates.Add(key, new DelegateInfo(funcPtr));
                return;
            }
#endif

            try
            {
                _delegates.Add(key, new DelegateInfo(method.MethodHandle.GetFunctionPointer()));
            }
            catch (NotSupportedException) when (IsRunningOnAndroid())
            {
                throw new PlatformNotSupportedException($"Cannot obtain function pointer {key}: it must be registered.");
            }
        }

        private static bool IsRunningOnAndroid()
        {
#if ANDROID
            return true;
#else
            return false;
#endif
        }

        private static string GetKey(MethodInfo info)
        {
            return $"{info.DeclaringType?.Name}.{info.Name}";
        }

        private static readonly SortedList<string, DelegateInfo> _delegates;

        unsafe static Delegates()
        {
            _delegates = new SortedList<string, DelegateInfo>();

#if ANDROID
            _androidDelegates = new Dictionary<string, Delegate>
            {
                // MathHelper delegates
                { "MathHelper.Abs", new MathHelperAbsDelegate(Math.Abs) },
                { "MathHelper.Ceiling", new MathHelperCeilingDelegate(Math.Ceiling) },
                { "MathHelper.Floor", new MathHelperFloorDelegate(Math.Floor) },
                { "MathHelper.Round", new MathHelperRoundDelegate((value, mode) => Math.Round(value, (MidpointRounding)mode)) },
                { "MathHelper.Truncate", new MathHelperTruncateDelegate(Math.Truncate) },
                
                // MathHelperF delegates
                { "MathHelperF.Abs", new MathHelperFAbsDelegate(MathF.Abs) },
                { "MathHelperF.Ceiling", new MathHelperFCeilingDelegate(MathF.Ceiling) },
                { "MathHelperF.Floor", new MathHelperFFloorDelegate(MathF.Floor) },
                { "MathHelperF.Round", new MathHelperFRoundDelegate((value, mode) => MathF.Round(value, (MidpointRounding)mode)) },
                { "MathHelperF.Truncate", new MathHelperFTruncateDelegate(MathF.Truncate) },
                
                // NativeInterface delegates
                { "NativeInterface.Break", new NativeInterfaceBreakDelegate(NativeInterface.Break) },
                { "NativeInterface.CheckSynchronization", new NativeInterfaceCheckSynchronizationDelegate(NativeInterface.CheckSynchronization) },
                { "NativeInterface.EnqueueForRejit", new NativeInterfaceEnqueueForRejitDelegate(NativeInterface.EnqueueForRejit) },
                { "NativeInterface.GetCntfrqEl0", new NativeInterfaceGetCntfrqEl0Delegate(NativeInterface.GetCntfrqEl0) },
                { "NativeInterface.GetCntpctEl0", new NativeInterfaceGetCntpctEl0Delegate(NativeInterface.GetCntpctEl0) },
                { "NativeInterface.GetCntvctEl0", new NativeInterfaceGetCntvctEl0Delegate(NativeInterface.GetCntvctEl0) },
                { "NativeInterface.GetCtrEl0", new NativeInterfaceGetCtrEl0Delegate(NativeInterface.GetCtrEl0) },
                { "NativeInterface.GetDczidEl0", new NativeInterfaceGetDczidEl0Delegate(NativeInterface.GetDczidEl0) },
                { "NativeInterface.GetFunctionAddress", new NativeInterfaceGetFunctionAddressDelegate(NativeInterface.GetFunctionAddress) },
                { "NativeInterface.InvalidateCacheLine", new NativeInterfaceInvalidateCacheLineDelegate(NativeInterface.InvalidateCacheLine) },
                { "NativeInterface.ReadByte", new NativeInterfaceReadByteDelegate(NativeInterface.ReadByte) },
                { "NativeInterface.ReadUInt16", new NativeInterfaceReadUInt16Delegate(NativeInterface.ReadUInt16) },
                { "NativeInterface.ReadUInt32", new NativeInterfaceReadUInt32Delegate(NativeInterface.ReadUInt32) },
                { "NativeInterface.ReadUInt64", new NativeInterfaceReadUInt64Delegate(NativeInterface.ReadUInt64) },
                { "NativeInterface.ReadVector128", new NativeInterfaceReadVector128Delegate(NativeInterface.ReadVector128) },
                { "NativeInterface.SignalMemoryTracking", new NativeInterfaceSignalMemoryTrackingDelegate(NativeInterface.SignalMemoryTracking) },
                { "NativeInterface.SupervisorCall", new NativeInterfaceSupervisorCallDelegate(NativeInterface.SupervisorCall) },
                { "NativeInterface.ThrowInvalidMemoryAccess", new NativeInterfaceThrowInvalidMemoryAccessDelegate(NativeInterface.ThrowInvalidMemoryAccess) },
                { "NativeInterface.Undefined", new NativeInterfaceUndefinedDelegate(NativeInterface.Undefined) },
                { "NativeInterface.WriteByte", new NativeInterfaceWriteByteDelegate(NativeInterface.WriteByte) },
                { "NativeInterface.WriteUInt16", new NativeInterfaceWriteUInt16Delegate(NativeInterface.WriteUInt16) },
                { "NativeInterface.WriteUInt32", new NativeInterfaceWriteUInt32Delegate(NativeInterface.WriteUInt32) },
                { "NativeInterface.WriteUInt64", new NativeInterfaceWriteUInt64Delegate(NativeInterface.WriteUInt64) },
                { "NativeInterface.WriteVector128", new NativeInterfaceWriteVector128Delegate(NativeInterface.WriteVector128) },
                
                // SoftFallback delegates
                { "SoftFallback.CountLeadingSigns", new SoftFallbackCountLeadingSignsDelegate(SoftFallback.CountLeadingSigns) },
                { "SoftFallback.CountLeadingZeros", new SoftFallbackCountLeadingZerosDelegate(SoftFallback.CountLeadingZeros) },
                { "SoftFallback.Crc32b", new SoftFallbackCrc32bDelegate(SoftFallback.Crc32b) },
                { "SoftFallback.Crc32cb", new SoftFallbackCrc32cbDelegate(SoftFallback.Crc32cb) },
                { "SoftFallback.Crc32ch", new SoftFallbackCrc32chDelegate(SoftFallback.Crc32ch) },
                { "SoftFallback.Crc32cw", new SoftFallbackCrc32cwDelegate(SoftFallback.Crc32cw) },
                { "SoftFallback.Crc32cx", new SoftFallbackCrc32cxDelegate(SoftFallback.Crc32cx) },
                { "SoftFallback.Crc32h", new SoftFallbackCrc32hDelegate(SoftFallback.Crc32h) },
                { "SoftFallback.Crc32w", new SoftFallbackCrc32wDelegate(SoftFallback.Crc32w) },
                { "SoftFallback.Crc32x", new SoftFallbackCrc32xDelegate(SoftFallback.Crc32x) },
                { "SoftFallback.Decrypt", new SoftFallbackDecryptDelegate(SoftFallback.Decrypt) },
                { "SoftFallback.Encrypt", new SoftFallbackEncryptDelegate(SoftFallback.Encrypt) },
                { "SoftFallback.FixedRotate", new SoftFallbackFixedRotateDelegate(SoftFallback.FixedRotate) },
                { "SoftFallback.HashChoose", new SoftFallbackHashChooseDelegate(SoftFallback.HashChoose) },
                { "SoftFallback.HashLower", new SoftFallbackHashLowerDelegate(SoftFallback.HashLower) },
                { "SoftFallback.HashMajority", new SoftFallbackHashMajorityDelegate(SoftFallback.HashMajority) },
                { "SoftFallback.HashParity", new SoftFallbackHashParityDelegate(SoftFallback.HashParity) },
                { "SoftFallback.HashUpper", new SoftFallbackHashUpperDelegate(SoftFallback.HashUpper) },
                { "SoftFallback.InverseMixColumns", new SoftFallbackInverseMixColumnsDelegate(SoftFallback.InverseMixColumns) },
                { "SoftFallback.MixColumns", new SoftFallbackMixColumnsDelegate(SoftFallback.MixColumns) },
                { "SoftFallback.PolynomialMult64_128", new SoftFallbackPolynomialMult64_128Delegate(SoftFallback.PolynomialMult64_128) },
                { "SoftFallback.SatF32ToS32", new SoftFallbackSatF32ToS32Delegate(SoftFallback.SatF32ToS32) },
                { "SoftFallback.SatF32ToS64", new SoftFallbackSatF32ToS64Delegate(SoftFallback.SatF32ToS64) },
                { "SoftFallback.SatF32ToU32", new SoftFallbackSatF32ToU32Delegate(SoftFallback.SatF32ToU32) },
                { "SoftFallback.SatF32ToU64", new SoftFallbackSatF32ToU64Delegate(SoftFallback.SatF32ToU64) },
                { "SoftFallback.SatF64ToS32", new SoftFallbackSatF64ToS32Delegate(SoftFallback.SatF64ToS32) },
                { "SoftFallback.SatF64ToS64", new SoftFallbackSatF64ToS64Delegate(SoftFallback.SatF64ToS64) },
                { "SoftFallback.SatF64ToU32", new SoftFallbackSatF64ToU32Delegate(SoftFallback.SatF64ToU32) },
                { "SoftFallback.SatF64ToU64", new SoftFallbackSatF64ToU64Delegate(SoftFallback.SatF64ToU64) },
                { "SoftFallback.Sha1SchedulePart1", new SoftFallbackSha1SchedulePart1Delegate(SoftFallback.Sha1SchedulePart1) },
                { "SoftFallback.Sha1SchedulePart2", new SoftFallbackSha1SchedulePart2Delegate(SoftFallback.Sha1SchedulePart2) },
                { "SoftFallback.Sha256SchedulePart1", new SoftFallbackSha256SchedulePart1Delegate(SoftFallback.Sha256SchedulePart1) },
                { "SoftFallback.Sha256SchedulePart2", new SoftFallbackSha256SchedulePart2Delegate(SoftFallback.Sha256SchedulePart2) },
                { "SoftFallback.SignedShrImm64", new SoftFallbackSignedShrImm64Delegate(SoftFallback.SignedShrImm64) },
                { "SoftFallback.Tbl1", new SoftFallbackTbl1Delegate(SoftFallback.Tbl1) },
                { "SoftFallback.Tbl2", new SoftFallbackTbl2Delegate(SoftFallback.Tbl2) },
                { "SoftFallback.Tbl3", new SoftFallbackTbl3Delegate(SoftFallback.Tbl3) },
                { "SoftFallback.Tbl4", new SoftFallbackTbl4Delegate(SoftFallback.Tbl4) },
                { "SoftFallback.Tbx1", new SoftFallbackTbx1Delegate(SoftFallback.Tbx1) },
                { "SoftFallback.Tbx2", new SoftFallbackTbx2Delegate(SoftFallback.Tbx2) },
                { "SoftFallback.Tbx3", new SoftFallbackTbx3Delegate(SoftFallback.Tbx3) },
                { "SoftFallback.Tbx4", new SoftFallbackTbx4Delegate(SoftFallback.Tbx4) },
                { "SoftFallback.UnsignedShrImm64", new SoftFallbackUnsignedShrImm64Delegate(SoftFallback.UnsignedShrImm64) },
                
                // SoftFloat delegates
                { "SoftFloat16_32.FPConvert", new SoftFloat16_32FPConvertDelegate(SoftFloat16_32.FPConvert) },
                { "SoftFloat16_64.FPConvert", new SoftFloat16_64FPConvertDelegate(SoftFloat16_64.FPConvert) },
                
                { "SoftFloat32.FPAdd", new SoftFloat32FPAddDelegate(SoftFloat32.FPAdd) },
                { "SoftFloat32.FPAddFpscr", new SoftFloat32FPAddFpscrDelegate(SoftFloat32.FPAddFpscr) },
                { "SoftFloat32.FPCompare", new SoftFloat32FPCompareDelegate(SoftFloat32.FPCompare) },
                { "SoftFloat32.FPCompareEQ", new SoftFloat32FPCompareEQDelegate(SoftFloat32.FPCompareEQ) },
                { "SoftFloat32.FPCompareEQFpscr", new SoftFloat32FPCompareEQFpscrDelegate(SoftFloat32.FPCompareEQFpscr) },
                { "SoftFloat32.FPCompareGE", new SoftFloat32FPCompareGEDelegate(SoftFloat32.FPCompareGE) },
                { "SoftFloat32.FPCompareGEFpscr", new SoftFloat32FPCompareGEFpscrDelegate(SoftFloat32.FPCompareGEFpscr) },
                { "SoftFloat32.FPCompareGT", new SoftFloat32FPCompareGTDelegate(SoftFloat32.FPCompareGT) },
                { "SoftFloat32.FPCompareGTFpscr", new SoftFloat32FPCompareGTFpscrDelegate(SoftFloat32.FPCompareGTFpscr) },
                { "SoftFloat32.FPCompareLE", new SoftFloat32FPCompareLEDelegate(SoftFloat32.FPCompareLE) },
                { "SoftFloat32.FPCompareLEFpscr", new SoftFloat32FPCompareLEFpscrDelegate(SoftFloat32.FPCompareLEFpscr) },
                { "SoftFloat32.FPCompareLT", new SoftFloat32FPCompareLTDelegate(SoftFloat32.FPCompareLT) },
                { "SoftFloat32.FPCompareLTFpscr", new SoftFloat32FPCompareLTFpscrDelegate(SoftFloat32.FPCompareLTFpscr) },
                { "SoftFloat32.FPDiv", new SoftFloat32FPDivDelegate(SoftFloat32.FPDiv) },
                { "SoftFloat32.FPMax", new SoftFloat32FPMaxDelegate(SoftFloat32.FPMax) },
                { "SoftFloat32.FPMaxFpscr", new SoftFloat32FPMaxFpscrDelegate(SoftFloat32.FPMaxFpscr) },
                { "SoftFloat32.FPMaxNum", new SoftFloat32FPMaxNumDelegate(SoftFloat32.FPMaxNum) },
                { "SoftFloat32.FPMaxNumFpscr", new SoftFloat32FPMaxNumFpscrDelegate(SoftFloat32.FPMaxNumFpscr) },
                { "SoftFloat32.FPMin", new SoftFloat32FPMinDelegate(SoftFloat32.FPMin) },
                { "SoftFloat32.FPMinFpscr", new SoftFloat32FPMinFpscrDelegate(SoftFloat32.FPMinFpscr) },
                { "SoftFloat32.FPMinNum", new SoftFloat32FPMinNumDelegate(SoftFloat32.FPMinNum) },
                { "SoftFloat32.FPMinNumFpscr", new SoftFloat32FPMinNumFpscrDelegate(SoftFloat32.FPMinNumFpscr) },
                { "SoftFloat32.FPMul", new SoftFloat32FPMulDelegate(SoftFloat32.FPMul) },
                { "SoftFloat32.FPMulFpscr", new SoftFloat32FPMulFpscrDelegate(SoftFloat32.FPMulFpscr) },
                { "SoftFloat32.FPMulAdd", new SoftFloat32FPMulAddDelegate(SoftFloat32.FPMulAdd) },
                { "SoftFloat32.FPMulAddFpscr", new SoftFloat32FPMulAddFpscrDelegate(SoftFloat32.FPMulAddFpscr) },
                { "SoftFloat32.FPMulSub", new SoftFloat32FPMulSubDelegate(SoftFloat32.FPMulSub) },
                { "SoftFloat32.FPMulSubFpscr", new SoftFloat32FPMulSubFpscrDelegate(SoftFloat32.FPMulSubFpscr) },
                { "SoftFloat32.FPMulX", new SoftFloat32FPMulXDelegate(SoftFloat32.FPMulX) },
                { "SoftFloat32.FPNegMulAdd", new SoftFloat32FPNegMulAddDelegate(SoftFloat32.FPNegMulAdd) },
                { "SoftFloat32.FPNegMulSub", new SoftFloat32FPNegMulSubDelegate(SoftFloat32.FPNegMulSub) },
                { "SoftFloat32.FPRecipEstimate", new SoftFloat32FPRecipEstimateDelegate(SoftFloat32.FPRecipEstimate) },
                { "SoftFloat32.FPRecipEstimateFpscr", new SoftFloat32FPRecipEstimateFpscrDelegate(SoftFloat32.FPRecipEstimateFpscr) },
                { "SoftFloat32.FPRecipStep", new SoftFloat32FPRecipStepDelegate(SoftFloat32.FPRecipStep) },
                { "SoftFloat32.FPRecipStepFused", new SoftFloat32FPRecipStepFusedDelegate(SoftFloat32.FPRecipStepFused) },
                { "SoftFloat32.FPRecpX", new SoftFloat32FPRecpXDelegate(SoftFloat32.FPRecpX) },
                { "SoftFloat32.FPRSqrtEstimate", new SoftFloat32FPRSqrtEstimateDelegate(SoftFloat32.FPRSqrtEstimate) },
                { "SoftFloat32.FPRSqrtEstimateFpscr", new SoftFloat32FPRSqrtEstimateFpscrDelegate(SoftFloat32.FPRSqrtEstimateFpscr) },
                { "SoftFloat32.FPRSqrtStep", new SoftFloat32FPRSqrtStepDelegate(SoftFloat32.FPRSqrtStep) },
                { "SoftFloat32.FPRSqrtStepFused", new SoftFloat32FPRSqrtStepFusedDelegate(SoftFloat32.FPRSqrtStepFused) },
                { "SoftFloat32.FPSqrt", new SoftFloat32FPSqrtDelegate(SoftFloat32.FPSqrt) },
                { "SoftFloat32.FPSub", new SoftFloat32FPSubDelegate(SoftFloat32.FPSub) },
                
                { "SoftFloat32_16.FPConvert", new SoftFloat32_16FPConvertDelegate(SoftFloat32_16.FPConvert) },
                
                { "SoftFloat64.FPAdd", new SoftFloat64FPAddDelegate(SoftFloat64.FPAdd) },
                { "SoftFloat64.FPAddFpscr", new SoftFloat64FPAddFpscrDelegate(SoftFloat64.FPAddFpscr) },
                { "SoftFloat64.FPCompare", new SoftFloat64FPCompareDelegate(SoftFloat64.FPCompare) },
                { "SoftFloat64.FPCompareEQ", new SoftFloat64FPCompareEQDelegate(SoftFloat64.FPCompareEQ) },
                { "SoftFloat64.FPCompareEQFpscr", new SoftFloat64FPCompareEQFpscrDelegate(SoftFloat64.FPCompareEQFpscr) },
                { "SoftFloat64.FPCompareGE", new SoftFloat64FPCompareGEDelegate(SoftFloat64.FPCompareGE) },
                { "SoftFloat64.FPCompareGEFpscr", new SoftFloat64FPCompareGEFpscrDelegate(SoftFloat64.FPCompareGEFpscr) },
                { "SoftFloat64.FPCompareGT", new SoftFloat64FPCompareGTDelegate(SoftFloat64.FPCompareGT) },
                { "SoftFloat64.FPCompareGTFpscr", new SoftFloat64FPCompareGTFpscrDelegate(SoftFloat64.FPCompareGTFpscr) },
                { "SoftFloat64.FPCompareLE", new SoftFloat64FPCompareLEDelegate(SoftFloat64.FPCompareLE) },
                { "SoftFloat64.FPCompareLEFpscr", new SoftFloat64FPCompareLEFpscrDelegate(SoftFloat64.FPCompareLEFpscr) },
                { "SoftFloat64.FPCompareLT", new SoftFloat64FPCompareLTDelegate(SoftFloat64.FPCompareLT) },
                { "SoftFloat64.FPCompareLTFpscr", new SoftFloat64FPCompareLTFpscrDelegate(SoftFloat64.FPCompareLTFpscr) },
                { "SoftFloat64.FPDiv", new SoftFloat64FPDivDelegate(SoftFloat64.FPDiv) },
                { "SoftFloat64.FPMax", new SoftFloat64FPMaxDelegate(SoftFloat64.FPMax) },
                { "SoftFloat64.FPMaxFpscr", new SoftFloat64FPMaxFpscrDelegate(SoftFloat64.FPMaxFpscr) },
                { "SoftFloat64.FPMaxNum", new SoftFloat64FPMaxNumDelegate(SoftFloat64.FPMaxNum) },
                { "SoftFloat64.FPMaxNumFpscr", new SoftFloat64FPMaxNumFpscrDelegate(SoftFloat64.FPMaxNumFpscr) },
                { "SoftFloat64.FPMin", new SoftFloat64FPMinDelegate(SoftFloat64.FPMin) },
                { "SoftFloat64.FPMinFpscr", new SoftFloat64FPMinFpscrDelegate(SoftFloat64.FPMinFpscr) },
                { "SoftFloat64.FPMinNum", new SoftFloat64FPMinNumDelegate(SoftFloat64.FPMinNum) },
                { "SoftFloat64.FPMinNumFpscr", new SoftFloat64FPMinNumFpscrDelegate(SoftFloat64.FPMinNumFpscr) },
                { "SoftFloat64.FPMul", new SoftFloat64FPMulDelegate(SoftFloat64.FPMul) },
                { "SoftFloat64.FPMulFpscr", new SoftFloat64FPMulFpscrDelegate(SoftFloat64.FPMulFpscr) },
                { "SoftFloat64.FPMulAdd", new SoftFloat64FPMulAddDelegate(SoftFloat64.FPMulAdd) },
                { "SoftFloat64.FPMulAddFpscr", new SoftFloat64FPMulAddFpscrDelegate(SoftFloat64.FPMulAddFpscr) },
                { "SoftFloat64.FPMulSub", new SoftFloat64FPMulSubDelegate(SoftFloat64.FPMulSub) },
                { "SoftFloat64.FPMulSubFpscr", new SoftFloat64FPMulSubFpscrDelegate(SoftFloat64.FPMulSubFpscr) },
                { "SoftFloat64.FPMulX", new SoftFloat64FPMulXDelegate(SoftFloat64.FPMulX) },
                { "SoftFloat64.FPNegMulAdd", new SoftFloat64FPNegMulAddDelegate(SoftFloat64.FPNegMulAdd) },
                { "SoftFloat64.FPNegMulSub", new SoftFloat64FPNegMulSubDelegate(SoftFloat64.FPNegMulSub) },
                { "SoftFloat64.FPRecipEstimate", new SoftFloat64FPRecipEstimateDelegate(SoftFloat64.FPRecipEstimate) },
                { "SoftFloat64.FPRecipEstimateFpscr", new SoftFloat64FPRecipEstimateFpscrDelegate(SoftFloat64.FPRecipEstimateFpscr) },
                { "SoftFloat64.FPRecipStep", new SoftFloat64FPRecipStepDelegate(SoftFloat64.FPRecipStep) },
                { "SoftFloat64.FPRecipStepFused", new SoftFloat64FPRecipStepFusedDelegate(SoftFloat64.FPRecipStepFused) },
                { "SoftFloat64.FPRecpX", new SoftFloat64FPRecpXDelegate(SoftFloat64.FPRecpX) },
                { "SoftFloat64.FPRSqrtEstimate", new SoftFloat64FPRSqrtEstimateDelegate(SoftFloat64.FPRSqrtEstimate) },
                { "SoftFloat64.FPRSqrtEstimateFpscr", new SoftFloat64FPRSqrtEstimateFpscrDelegate(SoftFloat64.FPRSqrtEstimateFpscr) },
                { "SoftFloat64.FPRSqrtStep", new SoftFloat64FPRSqrtStepDelegate(SoftFloat64.FPRSqrtStep) },
                { "SoftFloat64.FPRSqrtStepFused", new SoftFloat64FPRSqrtStepFusedDelegate(SoftFloat64.FPRSqrtStepFused) },
                { "SoftFloat64.FPSqrt", new SoftFloat64FPSqrtDelegate(SoftFloat64.FPSqrt) },
                { "SoftFloat64.FPSub", new SoftFloat64FPSubDelegate(SoftFloat64.FPSub) },
                
                { "SoftFloat64_16.FPConvert", new SoftFloat64_16FPConvertDelegate(SoftFloat64_16.FPConvert) }
            };
#endif

            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Abs)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Ceiling)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Floor)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Round)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Truncate)));

            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Abs)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Ceiling)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Floor)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Round)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Truncate)));

            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.Break)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.CheckSynchronization)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.EnqueueForRejit)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntfrqEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntpctEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntvctEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCtrEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetDczidEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.InvalidateCacheLine)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadByte)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt16)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt32)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt64)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadVector128)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.SignalMemoryTracking)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.SupervisorCall)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ThrowInvalidMemoryAccess)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.Undefined)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteByte)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt16)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt32)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt64)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteVector128)));

            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingSigns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingZeros)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32b)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cb)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32ch)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cw)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cx)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32h)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32w)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32x)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Decrypt)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Encrypt)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.FixedRotate)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashChoose)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashLower)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashMajority)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashParity)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashUpper)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.InverseMixColumns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.MixColumns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.PolynomialMult64_128)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShrImm64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl3)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl4)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx3)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx4)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShrImm64)));

            SetDelegateInfo(typeof(SoftFloat16_32).GetMethod(nameof(SoftFloat16_32.FPConvert)));
            SetDelegateInfo(typeof(SoftFloat16_64).GetMethod(nameof(SoftFloat16_64.FPConvert)));

            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPAddFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompare)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareEQ)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareEQFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGE)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGEFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGT)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGTFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLE)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLEFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLT)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLTFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPDiv)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMax)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxNum)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxNumFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMin)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinNum)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinNumFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMul)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulAddFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulSub)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulSubFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulX)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPNegMulAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPNegMulSub)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipEstimate)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipEstimateFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipStep)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipStepFused)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecpX)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtEstimate)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtEstimateFpscr)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtStep)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtStepFused)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPSqrt)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPSub)));

            SetDelegateInfo(typeof(SoftFloat32_16).GetMethod(nameof(SoftFloat32_16.FPConvert)));

            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPAddFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompare)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareEQ)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareEQFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGE)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGEFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGT)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGTFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLE)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLEFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLT)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLTFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPDiv)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMax)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxNum)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxNumFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMin)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinNum)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinNumFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMul)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulAddFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulSub)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulSubFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulX)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPNegMulAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPNegMulSub)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipEstimate)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipEstimateFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipStep)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipStepFused)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecpX)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtEstimate)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtEstimateFpscr)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtStep)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtStepFused)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPSqrt)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPSub)));

            SetDelegateInfo(typeof(SoftFloat64_16).GetMethod(nameof(SoftFloat64_16.FPConvert)));
        }
    }
}
