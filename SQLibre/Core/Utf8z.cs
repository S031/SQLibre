using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre.Core
{
    public readonly ref struct Utf8z
    {
        public static Utf8z Empty => new Utf8z();

        readonly ReadOnlySpan<byte> _data;

        public ref readonly byte GetPinnableReference() => ref _data.GetPinnableReference();

        public Utf8z(ReadOnlySpan<byte> data)
        {
            _data = data;
        }

        public Utf8z(string? data)
        {
            _data = FromString(data);
        }

        public int Length => _data.Length;

        public unsafe Utf8z(byte* p) : this(p == null ? ReadOnlySpan<byte>.Empty : FindZeroTerminator(p))
        {
        }

        public unsafe Utf8z(IntPtr p) : this(p == IntPtr.Zero ? ReadOnlySpan<byte>.Empty : FindZeroTerminator((byte*)(p.ToPointer())))
        {
        }

        unsafe private static long GetLen(byte* p)
        {
            var q = p;
            while (*q != 0)
            {
                q++;
            }
            return q - p;
        }

        private static unsafe ReadOnlySpan<byte> FindZeroTerminator(byte* p)
        {
            var len = (int)GetLen(p);
            return new ReadOnlySpan<byte>(p, len + 1);
        }

        public static ReadOnlySpan<byte> FromString(string? sourceText)
        {
            if (sourceText == null)
                return ReadOnlySpan<byte>.Empty;

            int nlen = Encoding.UTF8.GetByteCount(sourceText);

            var byteArray = new byte[nlen + 1];
            var wrote = Encoding.UTF8.GetBytes(sourceText, 0, sourceText.Length, byteArray, 0);
            byteArray[wrote] = 0;

            return byteArray;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int Rotl(int value, int shift)
        {
            // This is expected to be optimized into a single rotl instruction
            return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
        }

        public unsafe override int GetHashCode()
        {
            int length = _data.Length;
            int hash = length;
            fixed (byte* ap = _data)
            {
                byte* a = ap;

                while (length >= 4)
                {
                    hash = (hash + Rotl(hash, 5)) ^ *(int*)a;
                    a += 4; length -= 4;
                }
                if (length >= 2)
                {
                    hash = (hash + Rotl(hash, 5)) ^ *(short*)a;
                    a += 2; length -= 2;
                }
                if (length > 0)
                {
                    hash = (hash + Rotl(hash, 5)) ^ *a;
                }
                hash += Rotl(hash, 7);
                hash += Rotl(hash, 15);
                return hash;
            }
        }

        public bool Equals(Utf8z other)
        {
            int length = _data.Length;
            if (length != other.Length)
                return false;

            if (_data == other._data)
                return true;

            unsafe
            {
                fixed (byte* ap = _data) fixed (byte* bp = other._data)
                {
                    byte* a = ap;
                    byte* b = bp;

                    while (length >= 4)
                    {
                        if (*(int*)a != *(int*)b) return false;
                        a += 4; b += 4; length -= 4;
                    }
                    if (length >= 2)
                    {
                        if (*(short*)a != *(short*)b) return false;
                        a += 2; b += 2; length -= 2;
                    }
                    if (length > 0)
                    {
                        if (*a != *b) return false;
                    }
                    return true;
                }
            }
        }

        public override string? ToString()
        {
            if (_data.Length == 0)
            {
                return null;
            }

            unsafe
            {
                fixed (byte* q = _data)
                {
                    return Encoding.UTF8.GetString(q, _data.Length);
                }
            }
        }
        public byte[] ToArray() => _data.ToArray();
        public static int Compare(Utf8z strA, Utf8z strB)
        {
            int length = Math.Min(strA.Length, strB.Length);

            unsafe
            {
                fixed (byte* ap = strA._data)
                fixed (byte* bp = strB._data)
                {
                    byte* a = ap;
                    byte* b = bp;

                    while (length > 0)
                    {
                        if (*a != *b)
                            return *a - *b;
                        a += 1;
                        b += 1;
                        length -= 1;
                    }
                    return strA.Length - strB.Length;
                }
            }
        }

        public int CompareTo(Utf8z other) => Compare(this, other);

        public static implicit operator ReadOnlySpan<byte>(Utf8z z) => z._data;
        public static implicit operator string?(Utf8z z) => z.ToString();
        public static unsafe implicit operator byte*(Utf8z z)
        {
            fixed (byte* ap = z._data)
                return ap;
        }
        public static unsafe implicit operator IntPtr(Utf8z z)
        {
            fixed (byte* ap = z._data)
                return (IntPtr)ap;
        }
        public static implicit operator Utf8z(ReadOnlySpan<byte> z) => new Utf8z(z);
        public static implicit operator Utf8z(string? z) => new Utf8z(z);
        public static unsafe implicit operator Utf8z(byte* z) => new Utf8z(z);
        public static unsafe implicit operator Utf8z(IntPtr z) => new Utf8z(z);
        public static unsafe Utf8z FromPtr(byte* b) => new(b);
        public static Utf8z FromIntPtr(IntPtr handle) => new(handle);
        public static unsafe Utf8z FromPtrLen(byte* p, int length) => new Utf8z(new ReadOnlySpan<byte>(p, length));
    }
}
