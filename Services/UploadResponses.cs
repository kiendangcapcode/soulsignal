using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SoulSignal.Services;

public static class UploadResponses
{
    public static bool WantsJson(HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    public static object Errors(ModelStateDictionary state) => state
        .SelectMany(field => field.Value?.Errors.Select(error => new
        {
            field = field.Key,
            message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Thông tin chưa hợp lệ." : error.ErrorMessage
        }) ?? []).ToArray();
}
