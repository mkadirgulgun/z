using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection;
using z.Models;

namespace z.Controllers
{
    public class AdminController : Controller
    {
       

        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return false;
            }

            return true;
        }

        public IActionResult EditPost(int id)
        {
            if (CheckLogin())
            {
                ViewData["Id"] = HttpContext.Session.GetInt32("Id");

                using var connection = new SqlConnection(connectionString);
                var post = connection.QuerySingleOrDefault<Post>("SELECT posts.*, Username, Name, ProfileImg FROM posts LEFT JOIN Users ON posts.UserId = Users.Id WHERE posts.Id = @Id", new { Id = id });
                
                return View(post);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }
        [HttpPost]
        public IActionResult EditPost(Post model)
        {
            if (model.Detail == null)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalı işlem yaptın";
                return View("Message");
            }
            using var connection = new SqlConnection(connectionString);
            var imageName = model.ImgUrl;
            if (model.Image != null)
            {
                imageName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imageName);
                using var stream = new FileStream(path, FileMode.Create);
                model.Image.CopyTo(stream);
            }
            var sql = "UPDATE posts SET Detail = @Detail,ImgUrl = @ImgUrl, CreatedDate = @CreatedDate  WHERE Id=@Id";

            var parameters = new
            {
                model.Detail,
                model.Id,
                ImgUrl = imageName,
                CreatedDate = DateTime.Now,
            };

            var affectedRows = connection.Execute(sql, parameters);
            
            return RedirectToAction("Detail", "Home", new {id = model.Id});
        }
        public IActionResult DeletePost(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE FROM posts WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            return RedirectToAction("Index","Home");
        }
        public IActionResult ReportPost(int id)
        {
            using var connection = new SqlConnection(connectionString);
            
            var sql = "UPDATE posts SET ReportPost = 1 WHERE Id=@Id";

            var affectedRows = connection.Execute(sql, new { Id = id });

            return RedirectToAction("Index", "Home");

        }

        public IActionResult EditComment(int id)
        {
            if (CheckLogin())
            {
                ViewData["Id"] = HttpContext.Session.GetInt32("Id");

                using var connection = new SqlConnection(connectionString);
                var comment = connection.QuerySingleOrDefault<Post>("SELECT comments.*, Username, Name, ProfileImg FROM comments LEFT JOIN Users ON comments.UserId = Users.Id WHERE comments.Id = @Id", new { Id = id });
                
                return View(comment);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }
        [HttpPost]
        public IActionResult EditComment(Post model)
        {
            if (model.Comment == null)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalı işlem yaptın";
                return View("Message");
            }
            using var connection = new SqlConnection(connectionString);
            
            var sql = "UPDATE comments SET Comment = @Comment,CommentDate = @CommentDate WHERE Id=@Id";

            var parameters = new
            {
                model.Comment,
                model.Id,
                CommentDate = DateTime.Now
            };

            var affectedRows = connection.Execute(sql, parameters);

            return RedirectToAction("Detail", "Home", new { id = model.PostId});
        }
        public IActionResult DeleteComment(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE FROM comments WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            return RedirectToAction("Index", "Home");
        }   
        public IActionResult ReportComment(int id)
        {
            using var connection = new SqlConnection(connectionString);

            var sql = "UPDATE comments SET ReportComment = 1 WHERE Id=@Id";

            var affectedRows = connection.Execute(sql, new { Id = id });

            return RedirectToAction("Index", "Home");

        }
    }
}
