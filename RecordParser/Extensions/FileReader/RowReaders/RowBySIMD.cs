#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RecordParser.Extensions.FileReader.RowReaders;

internal abstract partial class RowBy
{
    protected readonly Vector128<byte> lineEndMaskVector = Vector128.Create((byte)'\n');
    protected readonly Vector128<byte> carriageReturnMaskVector = Vector128.Create((byte)'\r');
    protected readonly Vector128<byte> quoteMaskVector = Vector128.Create((byte)'"');

    protected readonly Vector256<byte> lineEndMaskVector256 = Vector256.Create((byte)'\n');
    protected readonly Vector256<byte> carriageReturnMaskVector256 = Vector256.Create((byte)'\r');
    protected readonly Vector256<byte> quoteMaskVector256 = Vector256.Create((byte)'"');
}
#endif