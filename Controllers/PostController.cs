using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class PostController(AppDbContext context, AuthService authService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly AuthService _authService = authService;

        // ────────────────────────────────────────────────────────────────
        // POST api/posts
        // Create a new post (for the authenticated teacher)
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpPost("posts")]
        public async Task<IActionResult> CreatePost([FromForm] Post request)
        {
            if (!ModelState.IsValid)
                return _authService.ValidationError(ModelState);

            // 1. Identify teacher from JWT
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });


            // 2. Set post details
            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                Tag = request.Tag,
                CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                TeacherId = teacher.Id
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Populate the Teacher navigation property for the response
            post.Teacher = teacher;

            return Ok(new
            {
                message = "تم إنشاء المنشور بنجاح.",
                post = MapPostResponse(post)
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/teachers/{teacherId}/posts
        // List all posts for a specific teacher (public)
        // ────────────────────────────────────────────────────────────────
       

        // ────────────────────────────────────────────────────────────────
        // GET api/posts/{postId}
        // Get a single post by id (public)
        // ────────────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("posts/{postId}")]
        public async Task<IActionResult> GetPost(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Teacher)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "المنشور غير موجود." });

            return Ok(new
            {
                message = "تم جلب المنشور بنجاح.",
                post = MapPostResponse(post)
            });
        }

        // ────────────────────────────────────────────────────────────────
        // DELETE api/posts/{postId}
        // Delete a post (only the teacher who created it can delete it)
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "المنشور غير موجود." });

            if (post.TeacherId != teacher.Id)
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف المنشور بنجاح." });
        }

        // ────────────────────────────────────────────────────────────────
        // Private helpers
        // ────────────────────────────────────────────────────────────────
        private object MapPostResponse(Post post) => new
        {
            post.Id,
            post.Title,
            post.Content,
            tag = post.Tag.ToString(),
            createdAt = ImpFunction.ToArabicNumbers(post.CreatedAt.ToString("yyyy/MM/dd")),
            teacher = post.Teacher == null ? null : new
            {
                post.Teacher.Id,
                post.Teacher.FullName,
                post.Teacher.Email,
                post.Teacher.Image,
                post.Teacher.Phone,
                post.Teacher.Government,
                post.Teacher.StageKind

            }
        };

     
    }
    }
