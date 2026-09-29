using Clinic_System.Data;
using Clinic_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Clinic_System.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Appointments/Book
        public IActionResult Book()
        {
            ViewData["SpecialtyId"] = new SelectList(_context.Specialties, "Id", "Name");
            ViewData["DoctorId"] = new SelectList(new List<Doctor>(), "Id", "Name");
            return View();
        }

        // POST: Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Appointment appointment, string PatientPhone)
        {
            appointment.AppointmentDate = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);

            bool isConflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == appointment.DoctorId &&
                a.AppointmentDate == appointment.AppointmentDate &&
                a.Status != AppointmentStatus.Cancelled);

            if (isConflict)
            {
                ModelState.AddModelError("", "Sorry, this time slot is already booked for this doctor. Please choose another time.");
            }

            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == PatientPhone);
                if (patient == null)
                {
                    patient = new Patient
                    {
                        Name = !string.IsNullOrEmpty(appointment.PatientName) ? appointment.PatientName : "Guest Patient",
                        Phone = !string.IsNullOrEmpty(PatientPhone) ? PatientPhone : "N/A",
                        CreatedAt = DateTime.Now
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    patient.Name = appointment.PatientName;
                    _context.Patients.Update(patient);
                    await _context.SaveChangesAsync();
                }

                appointment.PatientId = patient.Id;
                appointment.Status = AppointmentStatus.Waiting;

                _context.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment booked successfully!";
                return RedirectToAction("Index", "Home");
            }

            ViewData["SpecialtyId"] = new SelectList(_context.Specialties, "Id", "Name");
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "Id", "Name", appointment.DoctorId);
            return View(appointment);
        }

        // GET: 
        public async Task<IActionResult> MyAppointments(string patientName, string phone)
        {
            if (string.IsNullOrEmpty(patientName) || string.IsNullOrEmpty(phone))
            {
                return View(new List<Appointment>());
            }

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialty)
                .Include(a => a.Patient)
                .Where(a => a.PatientName.ToLower() == patientName.ToLower() && a.Patient.Phone == phone)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.PatientName = patientName;
            ViewBag.SearchPhone = phone;

            return View(appointments);
        }

        // GET: Appointments/Cancel/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status == AppointmentStatus.Waiting)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment cancelled successfully.";
            }

            return RedirectToAction(nameof(MyAppointments), new { patientName = appointment.PatientName, phone = appointment.Patient?.Phone });
        }

        // API 
        [HttpGet]
        public async Task<JsonResult> GetDoctorsBySpecialty(int specialtyId)
        {
            var doctors = await _context.Doctors
                .Where(d => d.SpecialtyId == specialtyId)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();
            return Json(doctors);
        }
    }
}