using DotEmilu.Samples.FullApp.DiContainers;
using DotEmilu.Samples.FullApp.Features;
using DotEmilu.Samples.FullApp.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFeatures()
    .AddDataAccess()
    .AddWebApi();

builder.Services.AddHostedService<InvoiceNotificationWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapInvoiceEndpoints();

await app.RunAsync();
