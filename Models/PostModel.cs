using System.ComponentModel.DataAnnotations;

namespace z.Models
{
	public class Post
	{
		public int Id { get; set; }
        public int? PostId { get; set; }
        public int? UserId  { get; set; }
        public string? Detail { get; set; }
		public DateTime? CreatedDate { get; set; }
		public string? UserName { get; set; }	
        public string? Name { get; set; }    
        public IFormFile? Image { get; set; }
        public IFormFile? ImageProfile { get; set; }
        public string? ImgUrl { get; set; }
        public string? ProfileImg { get; set; } 
        public string? CoverImg { get; set; }
        public string? Comment { get; set; }
        public DateTime? CommentDate { get; set; }
        public DateTime? JoiningDate { get; set; }
        public int? IsApproved { get; set; }
        public int? Likes { get; set; }
        public int? IsPublic { get; set; }
        public int? CommentCount { get; set; }
        public string? Email { get; set; }

    }
    public class PostComment
	{
        public int Id { get; set; }

        public int PostId { get; set; }
        public string Comment { get; set; }

        public DateTime CommentDate { get; set; }

        public List<Post>? Posts { get; set; }
    }       

    public class PostModel
    {
        public int Id { get; set; }
        public int IsApproved { get; set; }
        public Post Post { get; set; }

        public List<PostComment> Comments { get; set; }
        public List<Post> Posts { get; set; }

    }
    public class Search()
    {
        public string SearchUser { get; set; }
    }
}
