using Business.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Background service that polls Paymob every 60 seconds for any PaymentTransaction
/// records that are still Pending locally but have been completed on Paymob's side.
///
/// This is the self-healing fallback that fires even if:
///   - The POST webhook was never configured in the Paymob dashboard
///   - The GET redirect (redirection_url) did not reach the backend
///   - A network error interrupted the normal callback flow
///
/// Flow:
///   1. Find all PaymentTransactions with GatewayStatus=Pending AND older than 2 minutes
///   2. For each, call Paymob Intention API GET /v1/intention/{intentionId}/
///   3. If Paymob shows the intention has a successful transaction, call CompleteBookingWorkflowAsync
/// </summary>
public class PaymentSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentSyncService> _logger;

    // Only sync payments older than this to avoid race conditions with fresh initiations
    private static readonly TimeSpan MinAge = TimeSpan.FromMinutes(2);

    // How often to poll Paymob
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(60);

    public PaymentSyncService(IServiceScopeFactory scopeFactory, ILogger<PaymentSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║ PaymentSyncService STARTED                                             ║");
        _logger.LogInformation("║ Polling Interval: {Interval} seconds                                  ║", PollingInterval.TotalSeconds);
        _logger.LogInformation("║ Min Payment Age: {MinAge} minutes                                     ║", MinAge.TotalMinutes);
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");

        // Short initial delay to let the app finish starting up
        _logger.LogInformation("PaymentSyncService: Initial delay of 30 seconds before first sync");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PaymentSyncService: Cancellation requested during initial delay, stopping");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("PaymentSyncService: Starting sync cycle");
                await SyncPendingPaymentsAsync(stoppingToken);
                _logger.LogInformation("PaymentSyncService: Sync cycle completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PaymentSyncService: Cancellation requested, stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "╔══════════════════════════════════════════════════════════════════╗");
                _logger.LogCritical("║ PaymentSyncService: UNHANDLED EXCEPTION                              ║");
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
                _logger.LogInformation("PaymentSyncService: Waiting {Interval} seconds until next sync", PollingInterval.TotalSeconds);
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PaymentSyncService: Delay cancelled, stopping gracefully");
                break;
            }
        }

        _logger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║ PaymentSyncService STOPPED                                              ║");
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
    }

    private async Task SyncPendingPaymentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var bookingPaymentSvc = scope.ServiceProvider.GetRequiredService<IBookingPaymentService>();

        _logger.LogInformation("PaymentSyncService: running synchronization cycle...");
        try
        {
            var updatedCount = await bookingPaymentSvc.SyncPendingPaymentsAsync();
            if (updatedCount > 0)
            {
                _logger.LogInformation("PaymentSyncService: synchronization completed. {Count} payments successfully processed.", updatedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentSyncService: error running background synchronization");
        }
    }
}
