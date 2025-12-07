// File: Electric_Meter.MVVM.ViewModels/SystemOfParameterViewModel.cs (Đã cập nhật)

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Electric_Meter.Interfaces;
using Electric_Meter.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Electric_Meter.MVVM.ViewModels
{
    public partial class SystemOfParameterViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly LanguageService _languageService;
        private readonly IService _service;
        private readonly IServiceScopeFactory _scopeFactory;
        #endregion

        // **Observable Properties cho giá trị của các trường nhập liệu**
        [ObservableProperty] private string backupDbLocation;
        [ObservableProperty] private string databaseName;
        [ObservableProperty] private string account;
        [ObservableProperty] private string password;

        // **Observable Properties cho các chuỗi ngôn ngữ** (Phần hiện tại của bạn)
        #region [ Language Texts ]
        [ObservableProperty] private string backupDbLocationText;
        [ObservableProperty] private string accountText;
        [ObservableProperty] private string passwordText;
        [ObservableProperty] private string testText;
        [ObservableProperty] private string saveText;
        [ObservableProperty] private string syncText;
        [ObservableProperty] private string databaseNameText;
        // Bỏ thuộc tính backupDbNameText vì nó có vẻ dư thừa hoặc đã được thay bằng databaseNameText
        #endregion

        public SystemOfParameterViewModel(LanguageService languageService, IService service, IServiceScopeFactory serviceScopeFactory)
        {
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;
            _service = service;
            _scopeFactory = serviceScopeFactory;

            UpdateTexts();
            LoadParameters(); // Tải các tham số ngay khi khởi tạo
        }

        private void UpdateTexts()
        {
            BackupDbLocationText = _languageService.GetString("Backup db location");
            AccountText = _languageService.GetString("Account");
            PasswordText = _languageService.GetString("Password");
            TestText = _languageService.GetString("Test");
            SaveText = _languageService.GetString("Save");
            SyncText = _languageService.GetString("Sync");
            DatabaseNameText = _languageService.GetString("Database name");
        }

        // --- Logic Load/Save/Test ---

        // 1. Phương thức tải các thông số từ Service và cập nhật ViewModel
        private void LoadParameters()
        {
            try
            {
                var parameters = _service.LoadSystemParameters();
                if (parameters != null)
                {
                    BackupDbLocation = parameters.BackupDbLocation;
                    DatabaseName = parameters.DatabaseName;
                    Account = parameters.Account;
                    Password = parameters.Password;
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi tải dữ liệu
                Console.WriteLine($"Error loading parameters: {ex.Message}");
            }
        }

        // 2. Command để lưu thông số
        [RelayCommand]
        private void SaveConnect()
        {
            var parameters = new Models.SystemParameter
            {
                BackupDbLocation = BackupDbLocation,
                DatabaseName = DatabaseName,
                Account = Account,
                Password = Password
            };
            
            _service.SaveSystemParameters(parameters);
        }

        // 3. Command để kiểm tra kết nối
        [RelayCommand]
        private void TestConnect()
        {
            var parameters = new Models.SystemParameter
            {
                BackupDbLocation = BackupDbLocation,
                DatabaseName = DatabaseName,
                Account = Account,
                Password = Password
            };

            bool isConnected = _service.TestConnection(parameters);
            Console.WriteLine($"Connection Test: {(isConnected ? "Success" : "Failed")}");
        }

        // 4. Command đồng bộ (Nếu cần)
        [RelayCommand]
        private async Task Sync()
        {
            // Logic đồng bộ dữ liệu
            Console.WriteLine("Syncing data...");
        }
    }
}
