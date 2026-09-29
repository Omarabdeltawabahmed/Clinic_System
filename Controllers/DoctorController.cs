using Clinic_System.Data;
using Clinic_System.Models;
using Clinic_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_System.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class DoctorController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AiSummaryService _aiSummaryService;

        public DoctorController(AppDbContext db, AiSummaryService aiSummaryService)
        {
            _db = db;
            _aiSummaryService = aiSummaryService;
        }

        // Index action with date and doctor filtering
        public async Task<IActionResult> Index(int? doctorId, DateTime? filterDate)
        {
            var selectedDate = filterDate ?? DateTime.Today;

            var query = _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.MedicalRecord)
                .Where(a => a.AppointmentDate.Date == selectedDate.Date &&
                            a.Status != AppointmentStatus.Cancelled);

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);
            }

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.Doctors = await _db.Doctors.ToListAsync() ?? new List<Doctor>();
            ViewBag.SelectedDoctorId = doctorId;

            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.DisplayDateHeader = selectedDate.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> StartConsultation(int id)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.InProgress;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var appointment = await _db.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.NoShow;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateRecord(int appointmentId)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = appointment.Patient != null ? appointment.Patient.Name : appointment.PatientName;
            ViewBag.PatientId = appointment.PatientId;
            ViewBag.AppointmentId = appointmentId;

            var patientHistory = await _db.MedicalRecords
                .Include(m => m.Appointment)
                .Where(m => m.Appointment != null && m.Appointment.PatientId == appointment.PatientId)
                .OrderByDescending(m => m.Id)
                .ToListAsync();

            ViewBag.PatientHistory = patientHistory;

            var existingRecord = await _db.MedicalRecords.FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

            if (existingRecord != null)
            {
                return View(existingRecord);
            }

            var newRecord = new MedicalRecord
            {
                AppointmentId = appointmentId
            };

            return View(newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecord(MedicalRecord model)
        {
            ModelState.Remove("Appointment");
            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
                var existingRecord = await _db.MedicalRecords
                    .FirstOrDefaultAsync(m => m.AppointmentId == model.AppointmentId);

                if (existingRecord != null)
                {
                    existingRecord.Complaint = model.Complaint;
                    existingRecord.Diagnosis = model.Diagnosis;
                    existingRecord.Prescription = model.Prescription;
                    _db.MedicalRecords.Update(existingRecord);
                }
                else
                {
                    _db.MedicalRecords.Add(model);
                }

                var appointment = await _db.Appointments.FindAsync(model.AppointmentId);
                if (appointment != null)
                {
                    appointment.Status = AppointmentStatus.Done;
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var app = await _db.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == model.AppointmentId);
            ViewBag.PatientName = app?.Patient != null ? app.Patient.Name : app?.PatientName;
            ViewBag.PatientId = app?.PatientId;
            ViewBag.AppointmentId = model.AppointmentId;

            if (app?.PatientId != null)
            {
                ViewBag.PatientHistory = await _db.MedicalRecords
                    .Include(m => m.Appointment)
                    .Where(m => m.Appointment != null && m.Appointment.PatientId == app.PatientId)
                    .OrderByDescending(m => m.Id)
                    .ToListAsync();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientSummary(int patientId)
        {
            if (patientId <= 0)
                return BadRequest("Invalid patient ID.");

            var summary = await _aiSummaryService.GetPatientHistorySummaryAsync(patientId);
            return Json(new { summary });
        }
    }
}