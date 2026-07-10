using Business.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Background service that runs every hour and marks bookings as <see cref="BookingStatus.Expired"/>
/// when both signatures have not been completed within 7 days of contract generation.
/// </summary>
public class BookingExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationService> _logger;

    // Check interval — every hour is sufficient for a 7-day window
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    public BookingExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║ BookingExpirationService STARTED                                        ║");
        _logger.LogInformation("║ Check Interval: {Interval} hours                                      ║", CheckInterval.TotalHours);
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("BookingExpirationService: Starting expiration check cycle");
                await ExpireOverdueContractsAsync(stoppingToken);
                _logger.LogInformation("BookingExpirationService: Expiration check cycle completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BookingExpirationService: Cancellation requested, stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "╔══════════════════════════════════════════════════════════════════╗");
                _logger.LogCritical("║ BookingExpirationService: UNHANDLED EXCEPTION                          ║");
                _logger.LogCritical("╚══════════════════════════════════════════════════════════════════╝");
                _logger.LogCritical("Exception Type: {ExceptionType}", ex.GetType().FullName);
                _logger.LogCritical("Exception Message: {Message}", ex.Message);
                _logger.LogCritical("Exception StackTrace: {StackTrace}", ex.StackTrace);
                if (ex.InnerException != null)
                {
                    _logger.LogCritical("Inner Exception: {InnerMessage}", ex.InnerException.Message);
                    _logger.LogCritical("Inner StackTrace: {InnerStackTrace}", ex.InnerException.StackTrace);
                }
                // Continue running despite error - don't crash the application
            }

            try
            {
                _logger.LogInformation("BookingExpirationService: Waiting {Interval} hours until next check", CheckInterval.TotalHours);
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BookingExpirationService: Delay cancelled, stopping gracefully");
                break;
            }
        }

        _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║ BookingExpirationService STOPPED                                         ║");
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
    }

    private async Task ExpireOverdueContractsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Statuses that represent "waiting for signatures"
        var waitingStatuses = new[]
        {
            BookingStatus.WaitingForSignatures,
            BookingStatus.WaitingForStudentSignature,
            BookingStatus.WaitingForLandlordSignature
        };

        var now = DateTime.UtcNow;

        // Find bookings in waiting states that need expiration handling (7-day timeout)
        var expiredBookings = await unitOfWork.Bookings
            .GetAll()
            .Include(b => b.Contract)
            .Where(b =>
                waitingStatuses.Contains(b.BookingStatus) &&
                b.CreatedAt.AddDays(7) < now)
            .ToListAsync(ct);

        if (expiredBookings.Count == 0)
            return;

        _logger.LogInformation(
            "BookingExpirationService: found {Count} bookings to expire.", expiredBookings.Count);

        foreach (var booking in expiredBookings)
        {
            var previousStatus = booking.BookingStatus;

            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = now;
            await unitOfWork.Bookings.Update(booking);

            _logger.LogInformation(
                "Booking {BookingId} expired (was {PreviousStatus}). Created at {CreatedAt:u}.",
                booking.BookingId, previousStatus, booking.CreatedAt);

            // Notify the student (non-fatal)
            try
            {
                if (booking.Student?.UserId != null)
                {
                    await notificationService.SendRealTimeNotificationAsync(
                        booking.Student.UserId,
                        "Your booking contract has expired because signatures were not completed within 7 days. " +
                        "Please contact support to reactivate.",
                        "BookingExpired");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Non-fatal: failed to send expiration notification for booking {BookingId}.",
                    booking.BookingId);
            }
        }

        await unitOfWork.SaveChangesAsync();
        _logger.LogInformation("BookingExpirationService: expired {Count} bookings.", expiredBookings.Count);
    }
}
