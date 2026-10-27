using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoulSignal.Models;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectAfterLogin(returnUrl);
        return View(new LoginViewModel { ReturnUrl = LocalReturnUrl(returnUrl) });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl = LocalReturnUrl(model.ReturnUrl);
        if (!ModelState.IsValid) return View(model);
        var identifier = model.Email.Trim();
        var user = await users.FindByEmailAsync(identifier) ?? await users.FindByNameAsync(identifier);
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập, email hoặc mật khẩu chưa đúng. Nếu đã thử nhiều lần, vui lòng đợi vài phút.");
            return View(model);
        }
        if (model.ReturnUrl is not null) return LocalRedirect(model.ReturnUrl);
        if (await users.IsInRoleAsync(user!, AppRoles.Admin)) return RedirectToAction("Index", "Admin");
        if (await users.IsInRoleAsync(user!, AppRoles.Distributor)) return RedirectToAction("Index", "Distributor");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home") : View(new RegisterViewModel());

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        model.DisplayName = model.DisplayName?.Trim() ?? string.Empty;
        if (model.DisplayName.Length < 2 || model.DisplayName.Length > 80)
            ModelState.AddModelError(nameof(model.DisplayName), "Tên hiển thị cần từ 2 đến 80 ký tự sau khi bỏ khoảng trắng.");
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = model.DisplayName };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, AccountError(error));
            return View(model);
        }
        var roleResult = await users.AddToRoleAsync(user, AppRoles.Listener);
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Chưa thể tạo tài khoản. Vui lòng thử lại.");
            return View(model);
        }
        await signIn.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Tài khoản người nghe đã sẵn sàng. Chào mừng bạn đến Soul Signal!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private string? LocalReturnUrl(string? url) => !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? url : null;

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        var local = LocalReturnUrl(returnUrl);
        if (local is not null) return LocalRedirect(local);
        if (User.IsInRole(AppRoles.Admin)) return RedirectToAction("Index", "Admin");
        if (User.IsInRole(AppRoles.Distributor)) return RedirectToAction("Index", "Distributor");
        return RedirectToAction("Index", "Home");
    }

    private static string AccountError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Email này đã được sử dụng.",
        "PasswordTooShort" => "Mật khẩu cần ít nhất 10 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu cần có chữ số.",
        "PasswordRequiresLower" => "Mật khẩu cần có chữ thường.",
        "PasswordRequiresUpper" => "Mật khẩu cần có chữ in hoa.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần có ký tự đặc biệt.",
        _ => "Thông tin tài khoản chưa hợp lệ. Vui lòng kiểm tra và thử lại."
    };
}
