using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API.Controllers
{
    [ApiController]
    [Route("")]
    public class ReservationsController : ControllerBase
    {
        private readonly ReservationsContext _context;
        public record CreateReservationRequest(DateOnly Date, TimeOnly Time);

        public ReservationsController(ReservationsContext context)
        {
            _context = context;
        }

        private static readonly TimeOnly[] DefaultTimes =
        {
            new(9, 0),
            new(10, 0),
            new(11, 0)
        };

        private static Reservation CreateTimeSlot(DateOnly date, TimeOnly time)
        {
            return new Reservation
            {
                Date = date,
                Time = time,
                IsBooked = false
            };
        }


        [HttpGet("times")]
        public async Task<IActionResult> GetAvailableTimes([FromQuery] DateOnly date)
        {
            var hasAnyTimeSlots = await _context.Reservations.AnyAsync(r => r.Date == date);

            if (!hasAnyTimeSlots)
            {
                var timeSlots = DefaultTimes
                                .Select(time => CreateTimeSlot(date, time))
                                .ToArray();

                _context.Reservations.AddRange(timeSlots);
                await _context.SaveChangesAsync();
            }

            var availableTimes = await _context.Reservations
                                                .Where(r => r.Date == date && !r.IsBooked)
                                                .Select(r => r.Time)
                                                .ToListAsync();

            return Ok(availableTimes);
        }

        [HttpPost("booking")]
        public async Task<IActionResult> BookDateTime([FromBody] CreateReservationRequest request)
        {
            var slot = await _context.Reservations
                                        .SingleOrDefaultAsync(r => r.Date == request.Date && r.Time == request.Time);

            if (slot is null)
                return NotFound("Reserva nao disponivel para esta data.");

            if (slot.IsBooked)
                return Conflict("Horario ja esta reservado");

            slot.IsBooked = true;
            await _context.SaveChangesAsync();

            return Created($"/booking/{slot.Id}", slot);
        }

    }
}
