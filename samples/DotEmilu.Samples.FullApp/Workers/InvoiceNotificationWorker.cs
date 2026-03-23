using DotEmilu.Abstractions;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.Domain.Entities;

namespace DotEmilu.Samples.FullApp.Workers;

/// <summary>
/// Background service that periodically checks for unpaid invoices and "notifies"
/// about them. Demonstrates how to consume IHandler from a BackgroundService using
/// IServiceScopeFactory — necessary because handlers are scoped services.
/// </summary>
public sealed partial class InvoiceNotificationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<GetInvoicesRequest, PaginatedList<Invoice>>>();

                var request = new GetInvoicesRequest(PageNumber: 1, PageSize: 50);
                var result = await handler.HandleAsync(request, stoppingToken);

                if (result is not null)
                    LogFoundInvoices(result.Items.Count, result.PageNumber);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogWorkerError(ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Notification worker: found {Count} active invoices on page {Page}.")]
    private partial void LogFoundInvoices(int count, int page);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Notification worker encountered an error.")]
    private partial void LogWorkerError(Exception ex);
}
