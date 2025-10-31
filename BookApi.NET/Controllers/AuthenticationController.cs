using BookApi.NET.Controllers.Generated;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookApi.NET.Controllers;

[ApiController]
public class AuthenticationController : AuthenticationControllerBase
{
    public override async Task<IActionResult> Callback([BindRequired, FromQuery] string code, [BindRequired, FromQuery] string state)
    {
        throw new NotImplementedException("This callback method is not implemented");
    }
    public override Task<IActionResult> Login()
    {
        IActionResult result = Challenge(new AuthenticationProperties { RedirectUri = "/" },
            GoogleDefaults.AuthenticationScheme);

        return Task.FromResult(result);
    }

    [Authorize]
    public override async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }
}