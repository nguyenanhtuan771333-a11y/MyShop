using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BCrypt.Net;
using System.Linq;
using MyShop.Models;

namespace MyShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) { _db = db; }

        public IActionResult Index() { return RedirectToAction("Login"); }

        [HttpGet] public IActionResult Login() { return View(); }
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Sai ten dang nhap hoac mat khau";
            return View();
        }

        [HttpGet] public IActionResult Register() { return View(); }
        [HttpPost]
        public IActionResult Register(string username, string password, string confirm)
        {
            if (password != confirm) { ViewBag.Error = "Mat khau khong khop"; return View(); }
            if (_db.Users.Any(u => u.Username == username)) { ViewBag.Error = "Ten dang nhap da ton tai"; return View(); }
            var user = new User { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = "Member", DiamondBalance = 0 };
            _db.Users.Add(user); _db.SaveChanges(); return RedirectToAction("Login");
        }

        [HttpGet] public IActionResult ForgotPassword() { return View(); }
        [HttpPost]
        public IActionResult ForgotPassword(string username)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) { ViewBag.Error = "Khong tim thay nguoi dung"; return View(); }
            string token = Guid.NewGuid().ToString(); user.ResetToken = token; _db.SaveChanges();
            ViewBag.Token = token; ViewBag.Message = "Token khoi phuc: /Account/Reset?token=" + token; return View();
        }

        [HttpGet]
        public IActionResult Reset(string token)
        {
            var user = _db.Users.FirstOrDefault(u => u.ResetToken == token);
            if (user == null) return NotFound(); ViewBag.Token = token; return View();
        }
        [HttpPost]
        public IActionResult Reset(string token, string newPassword, string confirm)
        {
            var user = _db.Users.FirstOrDefault(u => u.ResetToken == token);
            if (user == null) return NotFound();
            if (newPassword != confirm) { ViewBag.Error = "Mat khau khong khop"; return View(); }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword); user.ResetToken = null; _db.SaveChanges(); return RedirectToAction("Login");
        }

        public IActionResult Logout() { HttpContext.Session.Clear(); return RedirectToAction("Login"); }
    }
}