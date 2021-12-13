using System;
using System.Buffers.Text;
using System.Text;

using Cursively;

namespace RecordParser.Benchmark
{
    public delegate void VisitPersonCallback(in Person person);

    public sealed class CursivelyPersonVisitor : CsvReaderVisitorBase
    {
        private readonly VisitPersonCallback _callback;

        private int _fieldIndex;

        private byte[] _dataBuf = new byte[1024];

        private int _dataBufUsed;

        private Person _person;

#if NET6_0_OR_GREATER
        private static readonly int MaxValidGenderLength = GetMaxEnumNameLength<Gender>();

        private readonly char[] _genderBuf = new char[MaxValidGenderLength];
        
        private static int GetMaxEnumNameLength<TEnum>() where TEnum : Enum
        {
            int maxSoFar = 0;
            foreach (string name in Enum.GetNames(typeof(TEnum)))
            {
                maxSoFar = Math.Max(maxSoFar, name.Length);
            }

            return maxSoFar;
        }
#endif

        public CursivelyPersonVisitor(VisitPersonCallback callback)
        {
            _callback = callback;
        }

        public override void VisitEndOfField(ReadOnlySpan<byte> chunk)
        {
            if (_dataBufUsed != 0)
            {
                VisitPartialFieldContents(chunk);
                chunk = _dataBuf.AsSpan(.._dataBufUsed);
                _dataBufUsed = 0;
            }

            switch (_fieldIndex++)
            {
                case 0:
                    _ = Utf8Parser.TryParse(chunk, out Guid g, out _);
                    _person.id = g;
                    break;

                case 1:
                    _person.name = Encoding.UTF8.GetString(chunk);
                    break;

                case 2:
                    _ = Utf8Parser.TryParse(chunk, out _person.age, out _);
                    break;

                case 3:
                    _ = Utf8Parser.TryParse(chunk, out _person.birthday, out _);
                    break;

                case 4:
#if NET6_0_OR_GREATER
                    Span<char> genderChars = _genderBuf.AsSpan(..Encoding.UTF8.GetChars(chunk, _genderBuf));
                    _person.gender = Enum.Parse<Gender>(genderChars);
#else
                    // N.B.: there are ways to improve the efficiency of this for earlier
                    // targets, but I think it's fine for performance-sensitive applications to
                    // have to upgrade to .NET 6.0 or higher...
                    _person.gender = Enum.Parse<Gender>(Encoding.UTF8.GetString(chunk));
#endif
                    break;

                case 5:
                    _person.email = Encoding.UTF8.GetString(chunk);
                    break;

                case 7:
                    _ = Utf8Parser.TryParse(chunk, out _person.children, out _);
                    break;
            }
        }

        public override void VisitEndOfRecord()
        {
            _callback(in _person);
            _person = default;
            _fieldIndex = 0;
        }

        public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
        {
            EnsureCapacity(_dataBufUsed + chunk.Length);
            chunk.CopyTo(_dataBuf.AsSpan(_dataBufUsed..));
            _dataBufUsed += chunk.Length;
        }

        private void EnsureCapacity(int neededLength)
        {
            if (_dataBuf.Length >= neededLength)
            {
                return;
            }

            int newLength = _dataBuf.Length;

            // assumption: no field is even close to 2 GiB in length.
            while (newLength < neededLength)
            {
                newLength += newLength;
            }

            Array.Resize(ref _dataBuf, newLength);
        }
    }
}
