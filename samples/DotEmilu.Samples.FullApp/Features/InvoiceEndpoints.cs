using DotEmilu.Samples.FullApp.Features.ConfirmInvoice;
using DotEmilu.Samples.FullApp.Features.CreateInvoice;
using DotEmilu.Samples.FullApp.Features.DeleteInvoice;
using DotEmilu.Samples.FullApp.Features.GetInvoiceById;
using DotEmilu.Samples.FullApp.Features.GetInvoices;
using DotEmilu.Samples.FullApp.Features.SyncInvoices;
using DotEmilu.Samples.FullApp.Features.UpdateInvoice;

namespace DotEmilu.Samples.FullApp.Features;

internal static class InvoiceEndpoints
{
    internal static WebApplication MapInvoiceEndpoints(this WebApplication app)
    {
        app.MapGroup("/invoices").WithTags("Invoices")
            .MapCreateInvoice()
            .MapConfirmInvoice()
            .MapSyncInvoices()
            .MapGetInvoices()
            .MapGetInvoiceById()
            .MapUpdateInvoice()
            .MapDeleteInvoice();

        return app;
    }
}
