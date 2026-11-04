using API.Controllers;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Tests;

public sealed class ReservationsTests : IDisposable
{
    private readonly string _connectionString;

    public ReservationsTests()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "test_connString")
            ?? throw new InvalidOperationException(
                "Defina \"test_connString\" para executar os testes.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.Database) ||
            !builder.Database.StartsWith("test_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"O banco '{builder.Database}' não começa com 'test_'. Teste cancelado.");
        }

        _connectionString = builder.ConnectionString;
    }

    [Fact]
    public async Task Database_RejectsDuplicateDateAndTime()
    {
        var options = new DbContextOptionsBuilder<ReservationsContext>()
            .UseNpgsql(_connectionString)
            .Options;

        var date = new DateOnly(2099, 1, 2);
        var time = new TimeOnly(10, 0);

        try
        {
            await using var context = new ReservationsContext(options);
            await context.Database.MigrateAsync();

            context.Reservations.Add(new Reservation
            {
                Date = date,
                Time = time,
                IsBooked = true
            });

            await context.SaveChangesAsync();

            context.Reservations.Add(new Reservation
            {
                Date = date,
                Time = time,
                IsBooked = true
            });

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());

            var postgresException =
                Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(
                PostgresErrorCodes.UniqueViolation,
                postgresException.SqlState);
        }
        finally
        {
            await using var cleanupContext = new ReservationsContext(options);

            await cleanupContext.Reservations
                .Where(r => r.Date == date && r.Time == time)
                .ExecuteDeleteAsync();
        }
    }
    [Fact]
    public async Task GetAvailableTimes_WhenDateHasNoSlots_CreatesAndReturnsThreeTimes()
    {
        var date = new DateOnly(2099, 2, 3);
        var options = new DbContextOptionsBuilder<ReservationsContext>()
            .UseNpgsql(_connectionString)
            .Options;

        try
        {
            await using var context = new ReservationsContext(options);
            await context.Database.MigrateAsync();

            var controller = new ReservationsController(context);

            var result = await controller.GetAvailableTimes(date);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var times = Assert.IsAssignableFrom<IEnumerable<TimeOnly>>(okResult.Value);

            Assert.Equal(
                new[] { new TimeOnly(9, 0), new TimeOnly(10, 0), new TimeOnly(11, 0) },
                times.OrderBy(time => time));
        }
        finally
        {
            await using var cleanupContext = new ReservationsContext(options);

            await cleanupContext.Reservations
                .Where(r => r.Date == date)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task BookDateTime_WhenSlotIsAvailable_BooksIt()
    {
        var date = new DateOnly(2099, 2, 4);
        var time = new TimeOnly(10, 0);
        var options = new DbContextOptionsBuilder<ReservationsContext>()
            .UseNpgsql(_connectionString)
            .Options;

        try
        {
            await using var context = new ReservationsContext(options);
            await context.Database.MigrateAsync();

            context.Reservations.Add(new Reservation
            {
                Date = date,
                Time = time,
                IsBooked = false
            });

            await context.SaveChangesAsync();

            var controller = new ReservationsController(context);
            var request = new ReservationsController.CreateReservationRequest(date, time);

            var result = await controller.BookDateTime(request);

            Assert.IsType<CreatedResult>(result);

            var savedReservation = await context.Reservations
                .SingleAsync(r => r.Date == date && r.Time == time);

            Assert.True(savedReservation.IsBooked);
        }
        finally
        {
            await using var cleanupContext = new ReservationsContext(options);

            await cleanupContext.Reservations
                .Where(r => r.Date == date && r.Time == time)
                .ExecuteDeleteAsync();
        }
    }

    public void Dispose() { }
}