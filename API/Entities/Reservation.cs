using Microsoft.EntityFrameworkCore;

namespace API.Entities
{
    //[Index(nameof(Date), nameof(Time))]
    public class Reservation
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public bool IsBooked { get; set; } = false;
    }
}
