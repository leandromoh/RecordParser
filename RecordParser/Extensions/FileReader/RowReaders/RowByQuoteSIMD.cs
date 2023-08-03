#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RecordParser.Extensions.FileReader.RowReaders;

internal partial class RowByQuote
{
    private Action AvaibleSIMD()
    {
        if (Avx2.IsSupported)
            return SIMD_Avx2;

        if (Sse2.IsSupported)
            return SIMD_Sse2;

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SIMD_Avx2()
    {
        // less 32-char to ensure we wont run out of data
        var end = bufferLength - 32;
        fixed (char* p = buffer)
        {
            var ip = (short*)p;
            while (i < end)
            {
                // load the current 32-char block into a SIMD vector 
                var v1 = Avx2.LoadVector256(ip + i);
                var v2 = Avx2.LoadVector256(ip + i + 16);
                var vx = Avx2.PackUnsignedSaturate(v1, v2);

                // search for special chars... result elements are either 0 (undetected) or ushort.MaxValue (detected)
                var hasSpecialChar =
                    Avx2.MoveMask(Avx2.CompareEqual(vx, quoteMaskVector256)) != 0 ||
                    Avx2.MoveMask(Avx2.CompareEqual(vx, carriageReturnMaskVector256)) != 0 ||
                    Avx2.MoveMask(Avx2.CompareEqual(vx, lineEndMaskVector256)) != 0;

                if (hasSpecialChar)
                    break;
                else
                    i += 32;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SIMD_Sse2()
    {
        // less 16-char to ensure we wont run out of data
        var end = bufferLength - 16;
        fixed (char* p = buffer)
        {
            var ip = (short*)p;
            while (i < end)
            {
                // load the current 16-char block into a SIMD vector 
                var v1 = Sse2.LoadVector128(ip + i);
                var v2 = Sse2.LoadVector128(ip + i + 8);
                var vx = Sse2.PackUnsignedSaturate(v1, v2);

                // search for special chars... result elements are either 0 (undetected) or ushort.MaxValue (detected)
                var hasSpecialChar =
                    Sse2.MoveMask(Sse2.CompareEqual(vx, quoteMaskVector)) != 0 ||
                    Sse2.MoveMask(Sse2.CompareEqual(vx, carriageReturnMaskVector)) != 0 ||
                    Sse2.MoveMask(Sse2.CompareEqual(vx, lineEndMaskVector)) != 0;

                if (hasSpecialChar)
                    break;
                else
                    i += 16;
            }
        }
    }
}
#endif
