using Clinic_System.Data;
using Clinic_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinic_System.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<IdentityUser> _userManager;

        public DoctorsController(AppDbContext db, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var doctors = await _db.Doctors.Include(d => d.Specialty).ToListAsync();
            return View(doctors);
        }

        // GET
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["SpecialtyId"] = new SelectList(_db.Specialties, "Id", "Name");
            return View();
        }

        // POST
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Doctor doctor, string email, string password, IFormFile? imageFile)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", $"The email '{email}' is already registered.");
                ViewData["SpecialtyId"] = new SelectList(_db.Specialties, "Id", "Name", doctor.SpecialtyId);
                return View(doctor);
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    doctor.ProfilePictureUrl = await SaveImageAsync(imageFile);
                }

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    doctor.UserId = user.Id;
                    _db.Doctors.Add(doctor);
                    await _db.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewData["SpecialtyId"] = new SelectList(_db.Specialties, "Id", "Name", doctor.SpecialtyId);
            return View(doctor);
        }

        // GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            ViewData["SpecialtyId"] = new SelectList(_db.Specialties, "Id", "Name", doctor.SpecialtyId);
            return View(doctor);
        }

        // POST
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,Salary,Fee,ProfilePictureUrl,SpecialtyId,UserId")] Doctor doctor, IFormFile? imageFile, bool deleteImage = false)
        {
            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (deleteImage && !string.IsNullOrEmpty(doctor.ProfilePictureUrl))
                    {
                        DeleteImage(doctor.ProfilePictureUrl); 
                        doctor.ProfilePictureUrl = null;        
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        DeleteImage(doctor.ProfilePictureUrl);
                        doctor.ProfilePictureUrl = await SaveImageAsync(imageFile);
                    }

                    _db.Update(doctor);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_db.Doctors.Any(d => d.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["SpecialtyId"] = new SelectList(_db.Specialties, "Id", "Name", doctor.SpecialtyId);
            return View(doctor);
        }

        // GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _db.Doctors.Include(d => d.Specialty).FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();

            return View(doctor);
        }

        // POST
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor != null)
            {
                if (!string.IsNullOrEmpty(doctor.UserId))
                {
                    var user = await _userManager.FindByIdAsync(doctor.UserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                DeleteImage(doctor.ProfilePictureUrl);

                _db.Doctors.Remove(doctor);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- Helper Methods ---

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "doctors");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/uploads/doctors/" + uniqueFileName;
        }

        private void DeleteImage(string? imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}