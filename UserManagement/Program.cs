
using Serilog;
using UserManagement.Extensions.Services;
using UserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.

builder.ConfigureServices();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using var scope = app.Services.CreateScope();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("api/health");
app.UseSerilogRequestLogging();
app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//using (var scopeLog = app.Services.CreateScope())
//{
//    Log.Information("JWT configurado:");
//    var configService = scopeLog.ServiceProvider.GetRequiredService<IConfigurationService>();

//    Log.Information("JWT configurado:");

//    try
//    {
//        var issuer = await configService.GetJwtIssuerAsync();
//        var audience = await configService.GetJwtAudienceAsync();

//        Log.Information("JWT configurado:");
//        Log.Information("Issuer: {Issuer}", issuer);
//        Log.Information("Audience: {Audience}", audience);
//    }
//    catch (Exception ex)
//    {
//        Log.Warning(ex, "No se pudo obtener configuración JWT completa");
//    }
//    // Use configService here  
//}


app.Run();
