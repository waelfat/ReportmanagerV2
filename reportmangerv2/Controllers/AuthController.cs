using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using reportmangerv2.Data;
using reportmangerv2.ViewModels;

namespace reportmangerv2.Controllers;

public class AuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public AuthController(
        IConfiguration configuration,
        ILogger<AuthController> logger,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        _configuration = configuration;
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded) 
            {
            var applicationUser = await _userManager.FindByEmailAsync(model.Email);
                if (applicationUser.IsActive)
                {
                _logger.LogInformation("User logged in.");
                return RedirectToAction("Index", "Home");
                    
                }
                await _signInManager.SignOutAsync();
                // inform user to conact admin
                ModelState.AddModelError(string.Empty, "Your account has not been activated yet. Please contact admin.");
            }
            
            
        }
        return View(model);
    }
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Auth");
    }
    public IActionResult AccessDenied()
    {
        return View();
    }

}
