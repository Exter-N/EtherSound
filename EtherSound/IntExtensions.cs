namespace EtherSound
{
    static class IntExtensions
    {
        static readonly int[] bitIndices =
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        public static int CountTrailingZeros(this int n)
        {
            if (n == 0)
            {
                return 32;
            }

            // Use a De Bruijn word to locate the bit
            uint res = unchecked((uint)(n & -n) * 0x077CB531U) >> 27;

            return bitIndices[res];
        }
    }
}
