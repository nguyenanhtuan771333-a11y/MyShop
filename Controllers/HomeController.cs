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
                if (user != null)
                {
                    user.DiamondBalance += recharge.DiamondReceived;
                    user.TotalRechargeCount += 1;
                    // Top nạp: member đứng đầu được quay
                    var top = _db.Users.Where(u => u.Role == "Member")
                                       .OrderByDescending(u => u.TotalRechargeCount)
                                       .FirstOrDefault();
                    if (top != null)
                    {
                        top.CanSpin = true;
                    }
                }
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

        // QUẢN LÝ MEMBER
        public IActionResult MemberList()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var admin = _db.Users.Find(userId.Value);
            if (admin == null || admin.Role != "Admin") return RedirectToAction("Index");

            var members = _db.Users.Where(u => u.Role == "Member").ToList();
            return View(members);
        }

        [HttpPost]
        public IActionResult DeleteMember(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var admin = _db.Users.Find(userId.Value);
            if (admin == null || admin.Role != "Admin") return RedirectToAction("Index");

            var user = _db.Users.Find(id);
            if (user != null && user.Role == "Member")
            {
                _db.Users.Remove(user);
                _db.SaveChanges();
            }
            return RedirectToAction("MemberList");
        }

        // TOP NẠP
        public IActionResult TopRechargers()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var admin = _db.Users.Find(userId.Value);
            if (admin == null || admin.Role != "Admin") return RedirectToAction("Index");

            var top = _db.Users.Where(u => u.Role == "Member")
                               .OrderByDescending(u => u.TotalRechargeCount)
                               .Take(10)
                               .ToList();
            return View(top);
        }

        // VÒNG QUAY MAY MẮN
        [HttpPost]
        public IActionResult SpinWheel()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { error = "Chưa đăng nhập" });
            var user = _db.Users.Find(userId.Value);
            if (user == null || !user.CanSpin) return Json(new { error = "Không có lượt quay" });

            var prizes = new[] { "10 KC", "20 KC", "50 KC", "100 KC", "200 KC", "500 KC", "Chúc may mắn lần sau!" };
            var prize = prizes[new Random().Next(prizes.Length)];

            user.CanSpin = false;
            if (int.TryParse(prize.Split(' ')[0], out int kc))
            {
                user.DiamondBalance += kc;
            }

            var history = new SpinHistory { UserId = user.Id, Prize = prize, SpinDate = DateTime.Now };
            _db.SpinHistories.Add(history);
            _db.SaveChanges();

            return Json(new { prize = prize, balance = user.DiamondBalance });
        }
    }
}