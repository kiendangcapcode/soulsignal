using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoulSignal.Models;
using SoulSignal.Services;

namespace SoulSignal.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Distributor)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class UploadProgressController(UploadProgressStore progress) : Controller
{
    [HttpPost]
    public IActionResult Start()
    {
        var id = progress.Start(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return id is null ? StatusCode(429, new { message = "Có quá nhiều lượt tải đang mở. Vui lòng đợi rồi thử lại." })
            : Json(new { uploadId = id });
    }

    [HttpGet]
    public IActionResult Status(string id)
    {
        var state = progress.Get(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return state is null ? NotFound() : Json(state);
    }

    [HttpPost]
    public IActionResult Cancel(string id)
    {
        var state = progress.Cancel(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return state is null ? NotFound() : Json(state);
    }
}
