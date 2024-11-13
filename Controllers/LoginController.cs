using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Net.Http;
using System;
using z.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace z.Controllers
{
    public class LoginController : Controller
    {
        

        public IActionResult Index()
        {
            ViewData["email"] = HttpContext.Session.GetString("email");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewData["UserName"] = HttpContext.Session.GetString("UserName");

            return View(new Login());
        }
        [HttpPost]
        public IActionResult Index(Login model)
        {
            if (model.Email == null || model.Password == null )
            {
                ViewData["Error"] = "Form eksik veya hatalı!";
                return View("Index", "Login");
            }
            model.Password = Helper.Hash(model.Password);
            using var connection = new SqlConnection(connectionString);
            var login = connection.Query<Login>("SELECT * FROM Users").ToList();

            foreach (var user in login)
            {
                if (user.Email == model.Email && user.Password == model.Password && user.IsApproved == 1)
                {
                    ViewData["Msg"] = "Giriş Başarılı";
                    HttpContext.Session.SetString("email", user.Email);
                    HttpContext.Session.SetInt32("Id", user.Id);
                    HttpContext.Session.SetString("UserName", user.UserName);

                    ViewBag.IdUser = user.Id;
                    return RedirectToAction("Index", "Home");

                }else if(user.Email == model.Email && user.Password == model.Password && user.IsApproved == 0)
                {
                    ViewData["Msg"] = "Hesabınızı onaylamadan giriş yapamazsınız. Onaylamak için mail kutunuza gidiniz.";
                }
                else
                {
                ViewData["Msg"] = "Kullanıcı adı veya şifre yanlış";
                }

            }
            return View("Index", model);
        }
        public IActionResult Exit()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
        public IActionResult SignUp(Login model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalı işlem yaptın";
                return View("Msg");
            }
            using var connection = new SqlConnection(connectionString);
            var login = connection.QueryFirstOrDefault<Login>("SELECT * FROM Users WHERE Email = @Email OR Username = @UserName", new { Email = model.Email, UserName = model.UserName });

            if(model.Password != model.PasswordRepeat)
            {
				ViewData["Message"] = "Sifreler uyusmuyor";
				return View("Index", model);
			}
            else if (login?.Email == model.Email)
            {
                ViewData["Message"] = "Bu mail kayıtlı";
                return View("Index",model);
            }
            else if (login?.UserName == model.UserName)
            {
                ViewData["Message"] = "Bu kullanıcı adı kayıtlı";
                return View("Index", model);
            }
            else 
            {
                model.Password = Helper.Hash(model.Password);

                var client = new SmtpClient("smtp.eu.mailgun.org", 587)
                {
                    Credentials = new NetworkCredential("postmaster@bildirim.mkadirgulgun.com.tr", "cb5edda1ad0913ef5144e9fc0f8484a2-fe9cf0a8-3d53c1ae"),
                    EnableSsl = true
                };
                var Key = Guid.NewGuid().ToString();

                var signup = "INSERT INTO users (Name,Username, Password, Email,ValidKey) VALUES (@Name,@UserName, @Password, @Email,@ValidKey)";

                var data = new
                {
                    model.Name,
                    model.UserName,
                    model.Password,
                    model.Email,
                    ValidKey = Key
                };

                var rowsAffected = connection.Execute(signup, data);

                ViewBag.Subject = "Hoş Geldiniz! Kayıt İşleminiz Başarıyla Tamamlandı";
                ViewBag.Body = $"<h1>Hoş Geldiniz, {model.Name}!</h1>\r\n            <p>Web sitemize kayıt olduğunuz için teşekkür ederiz. Kayıt işleminiz başarıyla tamamlandı.</p>\r\n            <p>Aşağıdaki bilgileri gözden geçirebilirsiniz:</p>\r\n            <ul>\r\n                <li><strong>Kullanıcı Adı:</strong> {model.UserName}</li>\r\n                <li><strong>E-posta:</strong> {model.Email}</li>\r\n            </ul>\r\n            <p>Hesabınızı doğrulamak ve hizmetlerimizden yararlanmaya başlamak için <a href=\"https://z.mkadirgulgun.com.tr/Login/Account/{Key}\">buraya tıklayın</a>.</p>\r\n            <p>İyi günler dileriz!</p>";
                ViewBag.MessageCssClass = "alert-success";
                ViewBag.Message = "Başarıyla kayıt olundu. Onaylamak için mail kutunuza gidin";
                ViewBag.Return = "Message";
                SendMail(model);
                return View("Msg");
            }
        }
        public IActionResult SendMail(Login model)
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
            return RedirectToAction(ViewBag.Return);

        }

        public IActionResult Account(string id)
        {
            using var connection = new SqlConnection(connectionString);
            var account = connection.QueryFirstOrDefault<Login>("SELECT * FROM Users WHERE ValidKey = @ValidKey", new { ValidKey = id });

            return View(account);
        }

        public IActionResult ConfirmAccount(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var students = connection.QueryFirstOrDefault<Login>("SELECT * FROM Users", new { Id = id });

            var sql = "UPDATE Users SET IsApproved = 1 WHERE Id = @Id";
            var affectedRows = connection.Execute(sql, new { Id = id });

            return RedirectToAction("Index");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ForgotPassword(Login model)
        {
            using var connection = new SqlConnection(connectionString);
            var login = connection.QueryFirstOrDefault<Login>("SELECT * FROM Users WHERE Email = @Email", new { model.Email });

            if (!(login == null))
            {
                var client = new SmtpClient("smtp.eu.mailgun.org", 587)
                {
                    Credentials = new NetworkCredential("postmaster@bildirim.mkadirgulgun.com.tr", "cb5edda1ad0913ef5144e9fc0f8484a2-fe9cf0a8-3d53c1ae"),
                    EnableSsl = true
                };
                var Key = Guid.NewGuid().ToString();
                var change = "UPDATE users SET ResetKey = @ResetKey WHERE Email=@Email";
                var parameters = new
                {
                    Email = login.Email,
                    ResetKey = Key
                };
                var affectedRows = connection.Execute(change, parameters);

                ViewBag.Subject = "Şifre Sıfırlama Talebiniz";
                ViewBag.Body = $"<p>Merhaba {model.UserName},</p>\r\n            <p>Şifrenizi sıfırlamak için bir talepte bulunduğunuzu aldık. Lütfen aşağıdaki bağlantıya tıklayarak şifrenizi sıfırlayın:</p>\r\n            <p><a href=\"https://z.mkadirgulgun.com.tr/Login/ResetPassword/{Key}\" class=\"button\">Şifreyi Sıfırla</a></p>\r\n            <p>Bu bağlantı, güvenliğiniz için 24 saat geçerli olacaktır. Eğer bu talebi siz yapmadıysanız, lütfen bu e-postayı dikkate almayın.</p>\r\n            <p>Şifrenizi sıfırlama konusunda herhangi bir sorun yaşarsanız, bizimle iletişime geçmekten çekinmeyin.</p>";
                ViewBag.MessageCssClass = "alert-success";
                ViewBag.Message = "Şifre Sıfırlama Talebiniz Başarıyla Alındı. Lütfen mail kutunuza gidin";
                ViewBag.Return = "Message";
                SendMail(model);
                return View("Msg");
            }
            else
            {
                @ViewData["Msg"] = "Bu E-Postaya ait bir kayıt bulunamadı.";
                return View();
            }
        }
        public IActionResult ResetPassword(string id)
        {
            using var connection = new SqlConnection(connectionString);
            var account = connection.QueryFirstOrDefault<Login>("SELECT * FROM Users WHERE ResetKey = @ResetKey", new { ResetKey = id });

            return View(account);
        }
        [HttpPost]
        public IActionResult ResetPassword(Login model)
        {
            
            using var connection = new SqlConnection(connectionString);
            model.Password = Helper.Hash(model.Password);

            var sql = "UPDATE Users SET Password = @Password WHERE Id=@Id";

            var parameters = new
            {
                model.Password,
                model.Id,
            };

            var affectedRows = connection.Execute(sql, parameters);
            ViewBag.Message = "Şifre Güncellendi.";
            ViewBag.MessageCssClass = "alert-success";
            ViewBag.Login = "Giris";
            return View("Msg");
        }
    }
}
