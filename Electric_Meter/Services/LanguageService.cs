using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Electric_Meter.Services
{
    public class LanguageService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _languages;
        private Dictionary<string, string> _currentLanguage;
        public LanguageService()
        {
            _languages = new Dictionary<string, Dictionary<string, string>>();

            LoadLanguage("en.json");
            LoadLanguage("vi.json");
            LoadLanguage("cn.json");
            LoadLanguage("km.json");

            // Đặt ngôn ngữ mặc định
            if (_languages.ContainsKey("en"))
            {
                _currentLanguage = _languages["en"];
            }
            else
            {
                // Xử lý lỗi nếu không nạp được ngôn ngữ
                Console.WriteLine("Default language 'en' not loaded.");
            }
        }

        private void LoadLanguage(string fileName)
        {
            // Đường dẫn tới thư mục chứa các file JSON
            var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
            var filePath = Path.Combine(directoryPath, fileName);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var languageCode = Path.GetFileNameWithoutExtension(fileName); // Lấy mã ngôn ngữ từ tên file

                _languages[languageCode] = translations; // Thêm vào từ điển
            }
            else
            {
                Console.WriteLine($"File not found: {filePath}");
            }
        }
        public void ChangeLanguage(string languageCode)
        {


            if (_languages.ContainsKey(languageCode))
            {
                _currentLanguage = _languages[languageCode];
            }
            else
            {
                // Xử lý lỗi, ví dụ: đặt ngôn ngữ mặc định hoặc ghi log
                _currentLanguage = _languages["en"]; // Ngôn ngữ mặc định
            }
        }

        public string GetString(string key)
        {
            if (_currentLanguage == null)
            {
                // Xử lý trường hợp ngôn ngữ hiện tại chưa được khởi tạo
                Console.WriteLine("Current language is not initialized.");
                return key; // Hoặc một giá trị mặc định khác
            }

            return _currentLanguage.ContainsKey(key) ? _currentLanguage[key] : key;
        }

    }
}
