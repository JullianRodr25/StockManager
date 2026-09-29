using StockManager.Infrastructure.Data;
using StockManager.Infrastructure.Services;
using StockManager.Infrastructure.Hosting;
using StockManager.Infrastructure.Notificaciones;
using StockManager.Application.Services;
using StockManager.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Channels;
using Scalar.AspNetCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Registrar servicios
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IProductoService>(sp => 
    new ProductoService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<IBarcodeService>(),
        sp.GetRequiredService<ICategoriaService>(),
        sp.GetRequiredService<IConfiguracionService>()));

// Registrar el HostedService para bootstrap del Admin inicial
builder.Services.AddHostedService<AdminBootstrapHostedService>();

// --- Notificaciones por WhatsApp + factura en PDF ---
// QuestPDF requiere aceptar explícitamente la licencia Community antes de generar cualquier documento.
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection("WhatsApp:Twilio"));

// Channel<DomainEvent> en memoria: el puente entre "algo pasó" (publicado desde el hilo de la
// petición HTTP) y "hay que avisar por WhatsApp" (consumido por el BackgroundService). Unbounded
// para que Publicar() nunca bloquee la transacción de negocio que lo dispara.
builder.Services.AddSingleton(Channel.CreateUnbounded<DomainEvent>());
builder.Services.AddSingleton<IEventoNotificacionPublisher, ChannelEventoNotificacionPublisher>();

builder.Services.AddScoped<IFacturaPdfService, QuestPdfFacturaService>();
builder.Services.AddScoped<IFacturaLinkTokenService, HmacFacturaLinkTokenService>();

builder.Services.AddHttpClient<IWhatsAppSender, TwilioWhatsAppSender>(client =>
{
    client.BaseAddress = new Uri("https://api.twilio.com/");
});

builder.Services.AddHostedService<WhatsAppNotificationBackgroundService>();

// Add OpenAPI/Swagger services
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddAuthorization();

// Configurar CORS para React frontends (web + PWA)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                  Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                  uri.Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Usar Scalar para documentación de API (reemplazo de Swagger)
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("StockManager API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Agregar CORS antes de autenticación y autorización
app.UseCors("FrontendDev");

// Agregar middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
