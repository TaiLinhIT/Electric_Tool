using System;
using System.Linq;

namespace Electric_Meter.Utilities
{
    public static class CRC
    {
        // Hàm chuyển chuỗi hex thành mảng byte
        public static byte[] ConvertHexStringToByteArray(string hex)
        {
            // Loại bỏ khoảng trắng và đảm bảo chuỗi được chuyển sang chữ hoa
            hex = hex.Replace(" ", "").ToUpper();

            // Kiểm tra nếu chuỗi có độ dài lẻ
            if (hex.Length % 2 != 0)
            {
                throw new FormatException("Hex string must have an even number of characters.");
            }

            // Khởi tạo mảng byte với độ dài bằng nửa độ dài chuỗi hex
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                // Chuyển mỗi cặp ký tự hex thành một byte
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        // Hàm tính CRC và trả về chuỗi CRC theo cặp byte phân cách bằng khoảng trắng
        public static string CalculateCRC(string request)
        {
            try
            {
                // Chuyển chuỗi request thành mảng byte
                byte[] requestBytes = ConvertHexStringToByteArray(request);

                // Tính toán CRC
                ushort crc = 0xFFFF; // Giá trị CRC ban đầu

                foreach (byte byteData in requestBytes)
                {
                    crc ^= byteData;
                    for (int i = 0; i < 8; i++)
                    {
                        if ((crc & 0x0001) != 0)
                        {
                            crc >>= 1;
                            crc ^= 0xA001;
                        }
                        else
                        {
                            crc >>= 1;
                        }
                    }
                }

                // Chuyển CRC thành chuỗi theo cặp byte (ví dụ: "47 41")
                string crcResult = crc.ToString("X4").ToUpper();  // Lấy kết quả CRC theo định dạng hex 4 ký tự
                string formattedCRC = string.Join(" ", new[] { crcResult.Substring(2, 2), crcResult.Substring(0, 2) });

                return formattedCRC; // Trả về CRC theo đúng định dạng cặp byte
            }
            catch (Exception ex)
            {
                throw new Exception("Error calculating CRC: " + ex.Message);
            }
        }
    }
}
