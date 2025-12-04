namespace Electric_Meter.Models
{
    public class ApiRequestQueue
    {
        public int Id { get; set; }
        public string RequestType { get; set; } 
        public string Endpoint { get; set; } 
        public string PayloadJson { get; set; } 

        public DateTime? ProcessedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastAttemptDate { get; set; }
        public int AttemptCount { get; set; } = 0; 
        public bool IsCompleted { get; set; } = false; 
        public bool IsFailed { get; set; } = false; 
        public string? ErrorMessage { get; set; } 
    }
}
