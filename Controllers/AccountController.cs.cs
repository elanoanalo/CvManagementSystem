using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CvManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Создаём роль если её ещё нет в AspNetRoles
            var roleName = model.Role.ToString();
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Добавляем пользователя в роль Identity
                await _userManager.AddToRoleAsync(user, roleName);
                await _signInManager.SignInAsync(user, isPersistent: true);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError(string.Empty, "Неверный email или пароль");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                    new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl ?? "/");
        }

        // ===== СОЦИАЛЬНЫЙ ЛОГИН =====

        // Шаг 1: Запуск OAuth — перенаправляем пользователя к провайдеру (Google/GitHub)
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            // Формируем URL на который провайдер вернёт пользователя после входа
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account",
                new { returnUrl });

            // Готовим настройки для перенаправления к провайдеру
            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            // Challenge — запускает редирект на страницу входа провайдера
            return Challenge(properties, provider);
        }

        // Шаг 2: Обработка возврата от провайдера
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null,
            string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            // Если провайдер вернул ошибку
            if (remoteError != null)
            {
                TempData["Error"] = $"Ошибка от внешнего провайдера: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            // Получаем информацию о входе от провайдера
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["Error"] = "Не удалось получить данные от провайдера";
                return RedirectToAction(nameof(Login));
            }

            // Пытаемся войти если этот внешний аккаунт уже привязан к пользователю
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                // Уже привязан — просто входим
                return LocalRedirect(returnUrl);
            }

            // Аккаунт не привязан — нужно создать или связать пользователя
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (email == null)
            {
                TempData["Error"] = "Провайдер не предоставил email";
                return RedirectToAction(nameof(Login));
            }

            // Проверяем — есть ли уже пользователь с таким email
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Пользователя нет — создаём нового
                var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

                user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Role = UserRole.Candidate // по умолчанию кандидат
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = "Не удалось создать пользователя";
                    return RedirectToAction(nameof(Login));
                }

                // Создаём роль если её нет и добавляем пользователя в неё
                var roleName = UserRole.Candidate.ToString();
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

                await _userManager.AddToRoleAsync(user, roleName);
            }

            // Привязываем внешний логин к пользователю (существующему или новому)
            await _userManager.AddLoginAsync(user, info);

            // Входим
            await _signInManager.SignInAsync(user, isPersistent: true);

            return LocalRedirect(returnUrl);
        }
    }
}