using System;
using System.IO;

namespace MulDeMu
{
    public class CDADPCM
    {
        private static int[] StepTable = new[]
        {
            28,    32,    36,    40,    44,    48,    52,    56,
			64,    68,    76,    84,    92,    100,   112,   124,
			136,   148,   164,   180,   200,   220,   240,   264,
			292,   320,   352,   388,   428,   472,   520,   572,
			628,   692,   760,   836,   920,   1012,  1116,  1228,
			1348,  1484,  1632,  1796,  1976,  2176,  2392,  2632,
			2896,  3184,  3504,  3852,  4240,  4664,  5128,  5644,
			6208,  6828,  7512,  8264,  9088,  9996,  10996, 12096,
			13308, 14640, 16104, 17712, 19484, 21432, 23576, 25936,
			28528, 31380, 32764, 32764, 32764, 32764, 32764, 32764,
			32764, 32764, 32764, 32764, 32764, 32764, 32764, 32764,
			32764
        };

        private static int[] IndexTable = new[]
        {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8
        };

        private static int[] InitTable = new[]
        {
            0x0800, 0x1800, 0x2800, 0x3800, 0x4800, 0x5800, 0x6800, 0x7800,
            0xF800, 0xE800, 0xD800, 0xC800, 0xB800, 0xA800, 0x9800, 0x8800
        };

        private static Int16 value;
        private static int index;

        public static byte[] DecodeBlock(byte[] data)
        {
            int ptr = 0;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            // Get initial sample
            value = BitConverter.ToInt16(data, ptr);
            bw.Write(value);
            ptr += 2;

            // Get initial index
            index = data[ptr];
            ptr += 2;

            // First byte has only the upper part used as a code
            bw.Write(GetSample(data[ptr] >> 4));
            ptr++;

            // Decode remaining data
            for (int i = 0; i < 31; i++)
            {
                bw.Write(GetSample(data[ptr] & 0x0F));
                bw.Write(GetSample(data[ptr++] >> 4));
            }

            return ms.ToArray();
        }

        private static Int16 GetSample(int code)
        {
            Int16 delta;

            Int16 val1 = (Int16)StepTable[index];
            Int16 val2 = (Int16)InitTable[code];
            delta = (Int16)((val1 * val2) >> 16);

            index += IndexTable[code];
            if (index < 0)
                index = 0;
            else if (index > 88)
                index = 88;

            Int32 tmp = value + delta;
            if (tmp > Int16.MaxValue)
            {
                tmp = Int16.MaxValue;
            }
            else if (tmp < Int16.MinValue)
            {
                tmp = Int16.MinValue;
            }

            value = (Int16)tmp;

            return value;
        }
    }
}
