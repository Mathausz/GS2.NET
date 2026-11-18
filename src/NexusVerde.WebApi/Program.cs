using NexusVerde.Api.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddNexusVerdeApi(builder.Configuration);

    var app = builder.Build();

    app.UseNexusVerdePipeline();
    app.MapNexusVerdeEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação falhou ao iniciar.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
