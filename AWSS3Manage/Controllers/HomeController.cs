using AWSS3Manage.Data;
using AWSS3Manage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;

namespace AWSS3Manage.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _db = db;
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginViewModel? model)
        {
            if (ModelState.IsValid)
            {
                AppUser? signedUser = await _userManager.FindByEmailAsync(model?.Email);
                if (signedUser == null)
                {
                    ViewBag.Message = "Error while logging in, please try again later.";
                }
                else
                {
                    var result = await _signInManager.PasswordSignInAsync(signedUser.UserName, model.Password, true, true);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(signedUser, isPersistent: true);
                        return RedirectToAction("UserFilesView", "File");
                    }
                    else
                    {
                        ViewBag.Message = "Wrong username or password!";
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterViewModel? model)
        {
            if (ModelState.IsValid)
            {
                AppUser? nameFindedUser = await _userManager.FindByNameAsync(model?.Username);
                AppUser? emailFindedUser = await _userManager.FindByEmailAsync(model?.Email);
                if (nameFindedUser != null)
                {
                    return BadRequest("Username Must Be Unique!");
                }

                if (emailFindedUser != null)
                {
                    return BadRequest("Email Must Be Unique!");
                }

                AppUser? user = new AppUser();
                user.UserName = model.Username!.Replace(" ", ""); ;
                user.UserDescription = model.UserDescription;
                user.Email = model.Email;
                string password = model.Password!.ToString();

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _db.SaveChangesAsync();
                    return RedirectToAction("Login", "Home");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("",item.Description);
                    }
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("UserFilesView", "File");
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}