using System; // ⭐ Đã thêm
using System.Linq; // ⭐ Đã thêm
using System.Net.Http;
using System.Text;
using System.Threading.Tasks; // ⭐ Đã thêm
using System.Timers; // Vẫn giữ, nhưng sẽ dùng tên đầy đủ cho Timer

using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Utilities;

using Newtonsoft.Json;

namespace Electric_Meter.Services
{
    public class BackgroundSyncService
    {
        private readonly IRequestQueueService _queueService;
        private readonly HttpClient _httpClient;

        // ⭐ Sửa lỗi: Dùng tên đầy đủ System.Timers.Timer
        private readonly System.Timers.Timer _syncTimer;

        // Cần truyền HttpClient vào để nó sử dụng BaseAddress đã cấu hình
        public BackgroundSyncService(IRequestQueueService queueService, HttpClient httpClient)
        {
            _queueService = queueService;
            _httpClient = httpClient;

            // Khởi tạo Timer: Cấu hình chạy mỗi 60 giây (60000ms)
            // ⭐ Sửa lỗi: Dùng tên đầy đủ System.Timers.Timer
            _syncTimer = new System.Timers.Timer(60000);
            _syncTimer.Elapsed += OnTimerElapsed;
            _syncTimer.AutoReset = true; // Lặp lại
        }

        public void Start()
        {
            Tool.Log("Bắt đầu Background Sync Worker...");
            _syncTimer.Start();
        }

        public void Stop()
        {
            Tool.Log("Dừng Background Sync Worker.");
            _syncTimer.Stop();
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Dừng timer trong khi đang xử lý để tránh chồng chéo các lần chạy
            _syncTimer.Stop();

            try
            {
                await RunSyncProcessAsync();
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi trong quá trình đồng bộ nền: {ex.Message}");
            }
            finally
            {
                // Khởi động lại timer sau khi hoàn thành
                _syncTimer.Start();
            }
        }

        private async Task RunSyncProcessAsync()
        {
            // Kiểm tra kết nối mạng đơn giản (cần cải thiện trong thực tế)
            // Trong môi trường WPF, việc kiểm tra kết nối mạng khá phức tạp,
            // nên ta dựa vào HttpRequestException để biết có kết nối không.

            Tool.Log("→ Bắt đầu kiểm tra hàng đợi đồng bộ...");
            // Đã có using System.Linq; nên .Any() và .Count() hoạt động
            var pendingRequests = await _queueService.GetPendingRequestsAsync();

            if (!pendingRequests.Any())
            {
                Tool.Log("Không có yêu cầu nào đang chờ đồng bộ.");
                return;
            }

            Tool.Log($"Tìm thấy {pendingRequests.Count()} yêu cầu đang chờ. Bắt đầu đồng bộ.");

            foreach (var request in pendingRequests)
            {
                // Chạy logic đồng bộ trong Task.Run để đảm bảo không chặn luồng timer
                // Lưu ý: Task.Run ở đây là không cần thiết nếu RunSyncProcessAsync đã là Task. RunSyncProcessAsync
                // đã chạy trong một luồng nền từ OnTimerElapsed, ta có thể await ProcessRequestAsync trực tiếp.
                // Tuy nhiên, nếu bạn muốn các ProcessRequestAsync chạy song song, cú pháp này có thể được giữ lại, 
                // nhưng hãy cân nhắc dùng Task.WhenAll nếu bạn muốn chờ tất cả hoàn thành. 
                // Giữ nguyên cú pháp hiện tại để đảm bảo tính tuần tự của hàng đợi.
                await ProcessRequestAsync(request);
            }
        }

        private async Task ProcessRequestAsync(ApiRequestQueue request)
        {
            try
            {
                HttpResponseMessage response;
                var content = new StringContent(request.PayloadJson, Encoding.UTF8, "application/json");

                switch (request.RequestType)
                {
                    case "POST":
                        Tool.Log($"Thử POST: {request.Endpoint}");
                        response = await _httpClient.PostAsync(request.Endpoint, content);
                        break;
                    case "PUT":
                        Tool.Log($"Thử PUT: {request.Endpoint}");
                        response = await _httpClient.PutAsync(request.Endpoint, content);
                        break;
                    case "DELETE":
                        // Giả định PayloadJson chứa ID cần xóa cho DELETE, cần xác minh lại cách bạn lưu trữ ID
                        // Ví dụ: Endpoint là "api/Device/" và PayloadJson là ID "5"
                        Tool.Log($"Thử DELETE: {request.Endpoint}{request.PayloadJson}");
                        response = await _httpClient.DeleteAsync($"{request.Endpoint}{request.PayloadJson}");
                        break;
                    default:
                        // Đã có using System; nên NotSupportedException hoạt động
                        throw new NotSupportedException($"Method {request.RequestType} not supported");
                }

                if (response.IsSuccessStatusCode)
                {
                    await _queueService.MarkRequestAsCompletedAsync(request);
                    Tool.Log($"✅ Đồng bộ thành công Request ID: {request.Id} ({request.RequestType} {request.Endpoint})");
                }
                else
                {
                    // Lỗi API (ví dụ: 404 Not Found, 400 Bad Request) -> đánh dấu Failed vĩnh viễn
                    var respContent = await response.Content.ReadAsStringAsync();
                    await _queueService.MarkRequestAsFailedAsync(request, $"API Status {response.StatusCode}: {respContent}");
                    Tool.Log($"❌ Đồng bộ thất bại (API): Request ID: {request.Id}. Status: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Lỗi Mạng/Kết nối -> tăng RetryCount và chờ lần chạy sau
                await _queueService.MarkRequestAsFailedAsync(request, $"Connection Error: {httpEx.Message}");
                Tool.Log($"⚠️ Đồng bộ tạm thời thất bại (Mạng): Request ID: {request.Id}. Sẽ thử lại sau.");
            }
            catch (Exception ex)
            {
                // Lỗi chung (Serialization, v.v.)
                await _queueService.MarkRequestAsFailedAsync(request, $"Internal Error: {ex.Message}");
                Tool.Log($"❌ Đồng bộ thất bại (Nội bộ): Request ID: {request.Id}. Lỗi: {ex.Message}");
            }
        }
    }
}
