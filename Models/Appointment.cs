using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clinic_System.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PatientName { get; set; } = string.Empty;

        // ربط الحجز بجدول الـ Doctor بدل الـ String العشوائي
        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        [ForeignKey("Doctor")]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required(ErrorMessage = "Appointment date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Appointment time is required.")]
        [Display(Name = "Appointment Time")]
        public TimeSpan AppointmentTime { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Waiting;

        public virtual MedicalRecord? MedicalRecord { get; set; }

        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
    }
}