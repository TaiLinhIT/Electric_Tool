using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Electric_Meter.Services
{
    public class LanguageService : ObservableObject
    {
        private Dictionary<string, string> _currentLanguage = new();

        public event Action LanguageChanged;

        public void LoadLanguage(string languageCode)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"Languages/{languageCode}.json");
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                _currentLanguage = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                LanguageChanged?.Invoke(); // Thông báo toàn app
            }
        }

        public string GetString(string key)
        {
            return _currentLanguage.TryGetValue(key, out var value) ? value : key;
        }
    }

}
