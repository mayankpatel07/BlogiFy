using BlogiFy.Data;
using BlogiFy.Models;
using BlogiFy.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace BlogiFy.Controllers
{
    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtension = {".jpg",".png",".jpeg" };

        public PostController(AppDbContext context , IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        [HttpGet]
        public IActionResult Index(int? categoryId)
        {
            var postQuery = _context.Posts.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
            {
                postQuery = postQuery.Where(p=>p.CategoryId == categoryId);
            }
            var posts = postQuery.ToList();
            ViewBag.Categories = _context.Categories.ToList();
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            if(id == null)
            {
                return NoContent();
            }
            var post =_context.Posts.Include(p=>p.Category).Include(p=>p.Comments).FirstOrDefault(p=>p.Id == id);

            if(post == null)
            {
                return NotFound();
            }
            return View(post);
        }


        [HttpGet]
        public IActionResult Create()
        {
            var postViewModel = new PostViewModel();
            postViewModel.Categories = _context.Categories.Select(c =>
                new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                }
            ).ToList();
            return View(postViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PostViewModel postViewModel)
        {
            if (ModelState.IsValid)
            {
                var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);
                if (!isAllowed) {
                    ModelState.AddModelError("", "Invalid Image Format . Allowd Format are .jpg, .jpeg, .png");
                    return View(postViewModel);
                }
                postViewModel.Post.FeatureImagePath = await UploadFiletoFolder (postViewModel.FeatureImage);
                await _context.AddAsync(postViewModel.Post);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            postViewModel.Categories = _context.Categories.Select(c =>
               new SelectListItem
               {
                   Value = c.Id.ToString(),
                   Text = c.Name,
               }
           ).ToList();
            return View(postViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var postFromDb = await _context.Posts.FirstOrDefaultAsync(p=>p.Id == id);

            if (postFromDb == null) {
                 return NotFound();
            }

            EditViewModel editViewModel = new EditViewModel
            {
                Post = postFromDb,
                Categories = _context.Categories.Select(c =>
                 new SelectListItem
                 {
                     Value = c.Id.ToString(),
                     Text = c.Name,
                 }
                 ).ToList(),

            };

            return View(editViewModel);
        }

        public JsonResult AddComment([FromBody]Comment comment)
        {
            comment.CommentDate= DateTime.Now;
            _context.Comments.Add(comment);
            _context.SaveChanges();
            return Json(new
            {
                username = comment.UserName,
                commentDate = comment.CommentDate.ToString("MMMM dd, yyyy"),
                content = comment.Content
            });
        }

        public async Task<string> UploadFiletoFolder(IFormFile file)
        {
            var inputFileExtension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString() + inputFileExtension;
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var imageFolderPath = Path.Combine(wwwRootPath, "images");

            if (!Directory.Exists(imageFolderPath))
            {
                Directory.CreateDirectory(imageFolderPath);
            }
            var filePath = Path.Combine(imageFolderPath, fileName);

            try
            {
                await using(var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                return "Error Uploding Images : " + ex.Message;
            }

            return "/images/"+ fileName;
        }

    } 
}
