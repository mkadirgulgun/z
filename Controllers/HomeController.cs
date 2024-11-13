using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Reflection;
using z.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace z.Controllers
{
    public class HomeController : Controller
    {
        


        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return false;
            }

            return true;
        }
        public IActionResult Index()
        {
            if (!CheckLogin())
            {

                return RedirectToAction("Index", "Login");
            }
            ViewData["email"] = HttpContext.Session.GetString("email");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            using var connection = new SqlConnection(connectionString);
            var posts = connection.Query<Post>("SELECT posts.*, Username, Name, ProfileImg FROM posts LEFT JOIN Users ON Users.Id = posts.UserId WHERE IsPublic = 1 ORDER BY CreatedDate DESC ").ToList();

            var user = connection.QuerySingleOrDefault<Post>("SELECT * FROM Users WHERE Id = @Id", new { Id = ViewData["Id"] });

            @ViewBag.User = user;
            return View(posts);
        }
        [Route("/profil/{UserName}")]
        public IActionResult Profile(string UserName)
        {



            using (var connection = new SqlConnection(connectionString))
            {
                ViewData["Id"] = HttpContext.Session.GetInt32("Id");
                ViewData["UserName"] = HttpContext.Session.GetString("UserName");

                var sql = "SELECT * FROM Users WHERE Username = @UserName";
                var profile = connection.QuerySingleOrDefault<Post>(sql, new { UserName });
                var posts = connection.Query<Post>("SELECT posts.*,Username,Name,ProfileImg, CoverImg FROM posts LEFT JOIN Users ON Users.Id = posts.UserId ORDER BY CreatedDate DESC").ToList();
                ViewBag.Posts = posts;
                ViewBag.Control = ViewData["Id"];

                return View(profile);
            }


        }
        public IActionResult EditProfile(int id)
        {

            if (CheckLogin())
            {
                ViewData["Id"] = HttpContext.Session.GetInt32("Id");
                ViewData["username"] = HttpContext.Session.GetString("UserName");

                using var connection = new SqlConnection(connectionString);
                var profile = connection.QuerySingleOrDefault<Post>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });

                return View(profile);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }
        [HttpPost]
        public IActionResult EditProfile(Post model)
        {
            if (model.Name == null)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn";
                return View("Message");
            }
            using var connection = new SqlConnection(connectionString);

            var ProfileImage = model.ProfileImg;
            if (model.ImageProfile != null)
            {
                ProfileImage = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageProfile.FileName);

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", ProfileImage);
                using var stream = new FileStream(path, FileMode.Create);
                model.ImageProfile.CopyTo(stream);
            }
            

            var CoverImg = model.CoverImg;
            if (model.Image != null)
            {
                CoverImg = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);

                var coverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", CoverImg);
                using var coverStream = new FileStream(coverPath, FileMode.Create);
                model.Image.CopyTo(coverStream);
            }


            var sql = "UPDATE Users SET Name = @Name,ProfileImg = @ProfileImg, CoverImg = @CoverImg WHERE Id=@Id";

            var parameters = new
            {
                model.Name,
                model.Id,
                ProfileImg = ProfileImage,
                CoverImg,
            };

            var affectedRows = connection.Execute(sql, parameters);
            var user = "SELECT * FROM Users WHERE Id = @Id";
            var profile = connection.QuerySingleOrDefault<Post>(user, new { model.Id });
            return RedirectToAction(profile.UserName,"profil");
        }
        public IActionResult Detail(int id)
        {
            if (CheckLogin())
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }

                ViewData["Id"] = HttpContext.Session.GetInt32("Id");
                ViewData["UserName"] = HttpContext.Session.GetString("UserName");


                using var connection = new SqlConnection(connectionString);

                var sql = "SELECT posts.*, Username, Name, ProfileImg FROM posts LEFT JOIN Users ON Users.Id = posts.UserId WHERE posts.Id = @Id";
                var post = connection.QuerySingleOrDefault<Post>(sql, new { Id = id });


                var comments = connection.Query<Post>("SELECT comments.*, Name, Username, ProfileImg FROM comments LEFT JOIN Users ON Users.Id = comments.UserId WHERE comments.PostId = @Id AND comments.IsApproved = 1 ", new { Id = id }).ToList();

                var count = connection.QuerySingle<int>("SELECT COUNT (PostId) as CommentCount FROM comments Where PostId = @id", new { id });

                ViewBag.Count = count;
                ViewBag.Comments = comments;
                ViewBag.Control = ViewData["Id"];

                return View(post);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }
        public IActionResult SendMail(Post model)
        {
            var client = new SmtpClient("smtp.eu.mailgun.org", 587)
            {
                Credentials = new NetworkCredential("postmaster@bildirim.mkadirgulgun.com.tr", "cb5edda1ad0913ef5144e9fc0f8484a2-fe9cf0a8-3d53c1ae"),
                EnableSsl = true
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress("bildirim@z.com.tr", "Z.com"),
                //ReplyTo = new MailAddress("info@mkadirgulgun.com.tr", "Mehmet Kadir Gülgün"),
                Subject = ViewBag.Subject,
                Body = ViewBag.Body,
                IsBodyHtml = true,
            };

            mailMessage.ReplyToList.Add(model.Email);
            //mailMessage.To.Add("mkadirgulgun@gmail.com");
            mailMessage.To.Add(new MailAddress($"{model.Email}", $"{model.UserName}"));

            client.Send(mailMessage);
            return View();

        }
        [HttpPost]
        public IActionResult AddComments(Post model)
        {
            if (model.Comment == null || model.PostId == null)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn";
                return View("Message");
            }
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            var userId = ViewData["Id"];

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO comments (Comment, PostId, UserId) VALUES (@Comment, @PostId, @userId)";
            try
            {
                var data = new
                {
                    model.Comment,
                    model.PostId,
                    userId
                };
                var affectedRows = connection.Execute(sql, data);


                var email = "SELECT posts.*, Username, Email FROM posts LEFT JOIN Users ON posts.UserId = Users.Id WHERE posts.Id = @PostId";
                var post = connection.QuerySingleOrDefault<Post>(email, new { model.PostId });
                ViewBag.Subject = "Tweetinize Yeni Yorum Yapýldý";
                ViewBag.Body = $"<h1>Tweetinize Yeni Yorum Yapýldý</h1>\r\n    <p>Merhaba {post.UserName},</p>\r\n    <p>Tweetinize yeni bir yorum yapýldý! Yorum detaylarý aþaðýda yer almaktadýr:</p>\r\n    <h2>Tweetiniz:</h2>\r\n    <p>\"{post.Detail}\"</p>\r\n    <h2>Yorum:</h2>\r\n    <p>\"{model.Comment}\"</p>\r\n    <p><strong>Yorum Yapan:</strong> {ViewData["UserName"]}</p>\r\n    <p>Tweetinizi ve yorumu görüntülemek için aþaðýdaki baðlantýya týklayabilirsiniz:</p>\r\n    <a href=\"https://z.mkadirgulgun.com.tr/Home/Detail/{post.Id}\">Tweeti Görüntüle</a>\r\n    <p>Herhangi bir sorunuz veya geri bildiriminiz olursa bize bildirin.</p>\r\n    <p>Teþekkürler,<br>Z Ekibi</p>";

                SendMail(post);
                return RedirectToAction("Detail", new { id = model.PostId });
            }
            catch
            {
                return RedirectToAction("Index");

            }
        }
        [HttpPost]
        public IActionResult AddPosts(Post model)
        {
            if (model.Detail == null)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn";
                return View("Message");
            }
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            var id = ViewData["Id"];

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO posts (Detail, ImgUrl, UserId,IsPublic) VALUES (@Detail, @ImgUrl, @id,@IsPublic)";
            if (model.Image != null)
            {

                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imageName);
                using var stream = new FileStream(path, FileMode.Create);
                model.Image.CopyTo(stream);
                model.ImgUrl = imageName;
            }
            var data = new
            {
                model.Detail,
                model.ImgUrl,
                model.IsPublic,
                id
            };
            var affectedRows = connection.Execute(sql, data);

            return RedirectToAction("Index");
        }
        public IActionResult ChangePassword()
        {
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            return View();
        }
        [HttpPost]
        public IActionResult ChangePassword(ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn";
                return View("Message");
            }
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            using var connection = new SqlConnection(connectionString);
            var password = connection.QuerySingleOrDefault<ChangePassword>("SELECT * FROM Users WHERE Id = @Id", new { Id = ViewData["Id"] });
            if (password.Password == model.OldPassword)
            {
                if (model.Password != model.PasswordRepeat)
                {
                    ViewBag.Password = "Deðiþtirmek istediðiniz þifreler uyuþmuyor";
                    return View();
                }
                var sql = "UPDATE Users SET Password = @Password WHERE Id=@Id";

                var parameters = new
                {
                    model.Password,
                    Id = ViewData["Id"]

                };

                var affectedRows = connection.Execute(sql, parameters);
                ViewBag.MessageCssClass = "alert-success";
                ViewBag.Message = "Þifre baþarýyla deðiþtirildi.";
                return View("Message");

            }
            else
            {
                ViewBag.Password = "Þifrenizi yanlýþ girdiniz..";
                return View();
            }


        }
        public IActionResult Search(Search model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalý iþlem yaptýn";
                return View("Message");
            }
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT * FROM Users WHERE Username LIKE @search";
            var post = connection.QuerySingleOrDefault<Post>(sql, new { search = model.SearchUser });
            if (post != null)
            {
                return RedirectToAction(post.UserName, "profil");
            }
            else
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Boyle bir kullanici bulunamadi..";
                return View("Message");
            }

        }
    }
}
