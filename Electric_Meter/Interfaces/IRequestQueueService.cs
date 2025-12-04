using System.Net.Http;

using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IRequestQueueService
    {
        Task EnqueueRequestAsync<T>(HttpMethod method, string endpoint, T data);
        Task<List<ApiRequestQueue>> GetPendingRequestsAsync();
        Task MarkRequestAsCompletedAsync(ApiRequestQueue request);
        Task MarkRequestAsFailedAsync(ApiRequestQueue request, string errorMessage);
    }
}
