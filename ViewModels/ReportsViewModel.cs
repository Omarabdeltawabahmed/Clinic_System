using System.Collections.Generic;

namespace Clinic_System.ViewModels
{
    public class ReportsViewModel
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int WaitingAppointments { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<string> SpecialtyNames { get; set; } = new();
        public List<decimal> SpecialtyRevenues { get; set; } = new();

        public List<string> DaysOfWeek { get; set; } = new();
        public List<int> DailyAppointments { get; set; } = new();
    }
}