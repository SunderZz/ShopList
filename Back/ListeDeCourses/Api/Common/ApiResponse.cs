using System.Diagnostics;

namespace ListeDeCourses.Api.Common
{
    public class ApiResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public string? Code { get; init; }
        public string? TraceId { get; init; }
        public object? Data { get; init; }

        public ApiResponse() { }

        public ApiResponse(bool success, object? data = null, string? message = null, string? code = null, string? traceId = null)
        {
            Success = success;
            Data = data;
            Message = message;
            Code = code;
            TraceId = traceId ?? GetCurrentTraceId();
        }

        public static ApiResponse Ok(object? data = null, string? message = null) =>
            new(true, data, message, traceId: GetCurrentTraceId());

        public static ApiResponse Fail(string message, string? code = null, string? traceId = null) =>
            new(false, null, message, code, traceId ?? GetCurrentTraceId());

        protected static string? GetCurrentTraceId() =>
            Activity.Current?.Id ?? Activity.Current?.RootId;
    }


    public class ApiResponse<T> : ApiResponse
    {
        public new T? Data { get; init; }

        public ApiResponse() { }

        public ApiResponse(bool success, T? data = default, string? message = null, string? code = null, string? traceId = null)
            : base(success, data, message, code, traceId)
        {
            Data = data;
        }

        public static ApiResponse<T> Ok(T? data = default, string? message = null) =>
            new(true, data, message, traceId: GetCurrentTraceId());

        public static new ApiResponse<T> Fail(string message, string? code = null, string? traceId = null) =>
            new(false, default, message, code, traceId ?? GetCurrentTraceId());
    }
}
