using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Utilities
{
    public class SoftCRC16Handle
    {
        public static bool CheckCRC16(byte[] value, byte CH, byte CL)
        {
            if (value == null)
            {
                return false;
            }

            if (value.Length < 2)
            {
                return false;
            }

            int num = value.Length;
            byte[] array = new byte[num - 2];
            Array.Copy(value, 0, array, 0, array.Length);
            byte[] array2 = CRC16(array, CH, CL);
            if (array2[num - 2] == value[num - 2] && array2[num - 1] == value[num - 1])
            {
                return true;
            }

            return false;
        }

        public static byte[] CRC16(byte[] value)
        {
            return CRC16(value, 160, 1);
        }

        public static byte[] CRC16(byte[] value, byte CH, byte CL, byte preH = byte.MaxValue, byte preL = byte.MaxValue)
        {
            byte[] array = new byte[value.Length + 2];
            value.CopyTo(array, 0);
            byte b = preL;
            byte b2 = preH;
            for (int i = 0; i < value.Length; i++)
            {
                b ^= value[i];
                for (int j = 0; j <= 7; j++)
                {
                    byte b3 = b2;
                    byte b4 = b;
                    b2 >>= 1;
                    b >>= 1;
                    if ((b3 & 1) == 1)
                    {
                        b = (byte)(b | 0x80u);
                    }

                    if ((b4 & 1) == 1)
                    {
                        b2 ^= CH;
                        b ^= CL;
                    }
                }
            }

            array[^2] = b;
            array[^1] = b2;
            return array;
        }
    }
}
