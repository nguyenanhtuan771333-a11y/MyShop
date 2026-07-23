using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyShop.Models;
using System.Linq;

namespace MyShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var user = _db.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role == "Admin") return RedirectToAction("AdminPanel");
            return RedirectToAction("MemberPanel");
        }

        public IActionResult MemberPanel()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var user = _db.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != "Member") return RedirectToAction("Index");

            ViewBag.Orders = _db.Orders.Where(o => o.UserId == userId).ToList();
            ViewBag.Recharges = _db.Recharges.Where(r => r.UserId == userId).ToList();
            ViewBag.User = user;

            return View();
        }

        [HttpPost]
        public IActionResult CreateOrder(int diamondAmount, decimal price, string gameId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = new Order
            {
                UserId = userId.Value,
                GameId = gameId,
                DiamondAmount = diamondAmount,
                Price = price,
                Status = "Chờ duyệt"
            };
            _db.Orders.Add(order);
            _db.SaveChanges();
            return RedirectToAction("MemberPanel");
        }

        [HttpPost]
        public IActionResult Recharge(decimal amount, int diamondReceived)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var recharge = new Recharge
            {
                UserId = userId.Value,
                Amount = amount,
                DiamondReceived = diamondReceived,
                Status = "Chờ duyệt"
            };
            _db.Recharges.Add(recharge);
            _db.SaveChanges();
            return RedirectToAction("MemberPanel");
        }

        public IActionResult AdminPanel()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var user = _db.Users.Find(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != "Admin") return RedirectToAction("Index");

            ViewBag.Orders = _db.Orders.Include(o => o.User).Where(o => o.Status == "Chờ duyệt").ToList();
            ViewBag.Recharges = _db.Recharges.Include(r => r.User).Where(r => r.Status == "Chờ duyệt").ToList();
            return View();
        }

        [HttpPost]
        public IActionResult ApproveOrder(int orderId)
        {
            var order = _db.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = "Thành công";
                var user = _db.Users.Find(order.UserId);
                if (user != null) user.DiamondBalance += order.DiamondAmount;
                _db.SaveChanges();
            }
            return RedirectToAction("AdminPanel");
        }

        [HttpPost]
        public IActionResult RejectOrder(int orderId)
        {
            var order = _db.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = "Thất bại";
                _db.SaveChanges();
            }
            return RedirectToAction("AdminPanel");
        }

        [HttpPost]
        public IActionResult ApproveRecharge(int rechargeId)
        {
            var recharge = _db.Recharges.Find(rechargeId);
            if (recharge != null)
            {
                recharge.Status = "Thành công";
                var user = _db.Users.Find(recharge.UserId);
                if (user != null) user.DiamondBalance += recharge.DiamondReceived;
                _db.SaveChanges();
            }
            return RedirectToAction("AdminPanel");
        }

        [HttpPost]
        public IActionResult RejectRecharge(int rechargeId)
        {
            var recharge = _db.Recharges.Find(rechargeId);
            if (recharge != null)
            {
                recharge.Status = "Thất bại";
                _db.SaveChanges();
            }
            return RedirectToAction("AdminPanel");
        }
    }
}