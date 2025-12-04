using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Electric_Meter.Interfaces;
using Electric_Meter.Models;

using Microsoft.EntityFrameworkCore; // Cần thêm namespace này cho ToListAsync
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace Electric_Meter.Services
{
    public class RequestQueueService : IRequestQueueService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RequestQueueService(IServiceScopeFactory serviceScopeFactory)
        {
            _scopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Thêm yêu cầu API mới vào hàng đợi.
        /// </summary>
        public async Task EnqueueRequestAsync<T>(HttpMethod method, string endpoint, T data)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            var request = new ApiRequestQueue
            {
                RequestType = method.ToString(),
                Endpoint = endpoint,
                PayloadJson = JsonConvert.SerializeObject(data),
                CreatedDate = DateTime.Now,
                // Đảm bảo các trường trạng thái mặc định được thiết lập nếu cần (ví dụ: IsCompleted = false, IsFailed = false)
                IsCompleted = false,
                IsFailed = false,
                ErrorMessage = null
            };

            await dbContext.ApiRequestQueues.AddAsync(request);
            await dbContext.SaveChangesAsync();
        }

        // -------------------------------------------------------------------
        // CÁC PHƯƠNG THỨC ĐÃ HOÀN THÀNH:
        // -------------------------------------------------------------------

        /// <summary>
        /// Lấy tất cả các yêu cầu đang chờ xử lý (chưa hoàn thành và chưa thất bại).
        /// </summary>
        public async Task<List<ApiRequestQueue>> GetPendingRequestsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            // Lấy các yêu cầu chưa hoàn thành (IsCompleted = false) và chưa bị đánh dấu thất bại (IsFailed = false)
            return await dbContext.ApiRequestQueues
                .Where(r => r.IsCompleted == false && r.IsFailed == false)
                .OrderBy(r => r.CreatedDate) // Xử lý theo thứ tự tạo
                .ToListAsync();
        }

        /// <summary>
        /// Đánh dấu một yêu cầu trong hàng đợi là đã hoàn thành thành công.
        /// </summary>
        public async Task MarkRequestAsCompletedAsync(ApiRequestQueue request)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            // Lấy request từ DB để đảm bảo chúng ta đang làm việc với entity được theo dõi
            var entity = await dbContext.ApiRequestQueues.FindAsync(request.Id);

            if (entity != null)
            {
                entity.IsCompleted = true;
                entity.ProcessedDate = DateTime.Now;
                // Đánh dấu là đã được sửa đổi
                dbContext.ApiRequestQueues.Update(entity);
                await dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Đánh dấu một yêu cầu trong hàng đợi là đã thất bại và lưu thông báo lỗi.
        /// </summary>
        public async Task MarkRequestAsFailedAsync(ApiRequestQueue request, string errorMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            // Lấy request từ DB để đảm bảo chúng ta đang làm việc với entity được theo dõi
            var entity = await dbContext.ApiRequestQueues.FindAsync(request.Id);

            if (entity != null)
            {
                entity.IsFailed = true;
                entity.ErrorMessage = errorMessage;
                entity.ProcessedDate = DateTime.Now; // Cập nhật thời gian xử lý/thất bại
                // Đánh dấu là đã được sửa đổi
                dbContext.ApiRequestQueues.Update(entity);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
