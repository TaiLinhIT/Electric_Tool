using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Electric_Meter.Utilities
{
    public class Tool
    {
        // đây là phương thức tĩnh
        public static int index = 0;
        public static string path()
        {
            return System.AppDomain.CurrentDomain.BaseDirectory;
        }
        public static string pathd()
        {
            return path() + @"Resources\";
        }
        /// <summary>
        /// Ghi log chi tiết cho các lỗi HttpRequestException
        /// </summary>
        public static void LogHttpRequestException(string endpoint, HttpRequestException httpEx)
        {
            Log($"❌ [HTTP Failed] Endpoint: {endpoint}. Lỗi: {httpEx.Message}");

            if (httpEx.InnerException != null)
            {
                Log($"   → Chi tiết: Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");

                // Xử lý lỗi SSL/Chứng chỉ cụ thể
                if (httpEx.InnerException is AuthenticationException)
                {
                    Log("   → Gợi ý: Lỗi SSL. Cần kiểm tra chứng chỉ máy chủ hoặc cấu hình HttpClient để bỏ qua chứng chỉ tự ký (self-signed) nếu dùng localhost/development.");
                }
            }
        }
        static readonly object obj = new object();
        public static void Log(string lg)
        {
            lock (obj)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = Path.Combine(baseDirectory, "Logs");

                // Tạo thư mục Logs nếu chưa tồn tại
                if (!Directory.Exists(logDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(logDirectory);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi tạo thư mục Logs: {ex.Message}");
                        return; // Thoát khỏi phương thức nếu không thể tạo thư mục
                    }
                }

                string logFileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
                string logFilePath = Path.Combine(logDirectory, logFileName);

                try
                {
                    using (StreamWriter w = File.AppendText(logFilePath))
                    {
                        w.WriteLine($"{DateTime.Now:HH:mm:ss} - {lg}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi ghi log: {ex.Message}");
                }
            }
        }
        public static string[] gCrc16Table;
        public static void Crc16()
        {
            string filepath = pathd() + "crc.json";
            string json = string.Empty;
            using (FileStream fs = new FileStream(filepath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("utf-8")))
                {
                    json = sr.ReadToEnd().ToString();
                }
            }
            gCrc16Table = JsonSerializer.Deserialize<string[]>(json);
        }

        //////////////
        public static int CALCRC16(int crc, int[] buf, int length)
        {
            int pos = 0;
            while (length != 0)
            {
                length -= 1;
                crc = Crc16Byte(crc, buf[pos]);
                pos += 1;
            }
            return crc;
        }
        public static int Crc16Byte(int crc, int data)
        {
            ushort u16 = Convert.ToUInt16(gCrc16Table[((crc >> 8 ^ data) & 0xff)], 16);
            return (crc << 8 ^ u16);
        }

        public static int Asc(string s)
        {
            if (!string.IsNullOrEmpty(s) && s.Length == 1)
            {
                ASCIIEncoding a = new ASCIIEncoding();
                int b = (int)a.GetBytes(s)[0];
                return b;
            }
            else
            {
                return 0;
            }
        }
        

        public static int convert_to_dec(int[] array)
        {
            int[] f = new int[3] { 0, 0, 0 };
            f[0] = array[0];
            f[1] = array[1];
            f[2] = array[2];
            int a = f[2] & 0xf0;
            a >>= 4;
            a = a * 10 + (f[2] & 0x0f);
            int a32 = a;
            a = f[1] & 0xf0;
            a >>= 4;
            a = a * 10 + (f[1] & 0x0f);
            int b32 = a;
            a32 = a32 + b32 * 100;
            a = f[0] & 0xf0;
            a >>= 4;
            a = a * 10 + (f[0] & 0x0f);
            b32 = a;
            a32 = a32 + b32 * 10000;
            Console.WriteLine(a32);
            return a32;
        }
        #region CRC校验相关的

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="buffer">数据</param>
        /// <param name="start">起点</param>
        /// <param name="len">长度</param>
        /// <returns>byte[]</returns>
        public static byte[] CRC_jy(byte[] buffer, UInt16 start, UInt16 len)
        {
            byte[] buffer2 = buffer.Take(len).ToArray();//数组复制

            byte[] buffer1 = SoftCRC16Handle.CRC16(buffer2);
            //Console.WriteLine("串口数据CRC_16:" + Byte_string(buffer1));
            return buffer1;
        }
        /// <summary>
        /// 比较当前byte数组与另一数组是否相等。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="target">需要比较的数组。</param>
        /// <returns></returns>
        public static bool EqualsBytes(byte[] obj, byte[] target)
        {
            if (obj.Length != target.Length)
                return false;
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i] != target[i])
                    return false;
            }
            return true;
        }
        //public static readonly object lockFlag = new object();
        /// <summary>
        /// 判断返回的CRC校验对不对
        /// </summary>
        /// <param name="A">数据值</param>
        /// <returns>bool(True/False)</returns>
        public static bool CRC_PD(byte[] A)
        {

            byte[] B = CRC_jy(A, 0, (ushort)(A.Length - 2));

            if (EqualsBytes(A, B))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public byte[] call_Crc16(byte[] buffer, int start = 0, int len = 0)
        {
            if (buffer == null || buffer.Length == 0) return null;
            if (start < 0) return null;
            if (len == 0) len = buffer.Length - start;
            int length = start + len;
            if (length > buffer.Length) return null;
            ushort crc = 0;// Initial value
            for (int i = start; i < length; i++)
            {
                crc ^= buffer[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) > 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);// 0xA001 = reverse 0x8005
                    else
                        crc = (ushort)(crc >> 1);
                }
            }
            byte[] ret = BitConverter.GetBytes(crc);
            Array.Reverse(ret);
            return ret;
        }
        #endregion
    }
}
