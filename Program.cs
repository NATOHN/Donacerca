using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Donacerca.Services;
using Scalar.AspNetCore;
using System.Text;
using Donacerca.Middleware;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Servicios

// AddSingleton crear una sola instancia de FirebaseService para toda la vida de la app
builder.Services.AddSingleton<FirebaseService>();

// AddScoped crea una instancia nueva por cada peticion HTTP que llegue
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<DonationService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NotificationService>();


builder.Services.AddControllers();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new BadRequestObjectResult(new
            {
                message = "Datos inválidos",
                errors
            });
        };
    });

// AddOpenApi registrar el generador de documentacion que Scalar va a leer
// !existe Scalar no va poder reconocer los endpoints que existen ni como los definieron
builder.Services.AddOpenApi(options =>
{
    // Registramos el transformer que agrega el botón de Bearer en Scalar
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Autenticacion JWT
// Le decimos a al app que el esquema de auth es JWT Bearer
// Bearer significa que el token ciaja en el header: Authorization: Bearer <token>
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Verificar que el token lo emitimos nosotros (app)
        ValidateIssuer = true,
        // Verificar que el token est para la misma app
        ValidateAudience = true,
        // Verificar que el token no ha expirado
        ValidateLifetime = true,
        // Verificar que la firma es valida
        ValidateIssuerSigningKey = true,
        // Verificar que estos valores coincidan con los que usamos para generar el token
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddHostedService<ExpiredPostsJob>();

// AddAuthorization, habilitar el uso del [Authorize] en los controllers
builder.Services.AddAuthorization();

// CORS
// CORS controla que origenes externos pueden llamar el API
// En desarrollo lo dejamos abierto para que Angular lo pueda consumir
// En produccion esto se restringe a dominios especificos
builder.Services.AddCors(options =>
{
   options.AddPolicy("AllowAll", policy =>
   {
       policy.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
   }); 
});

var app = builder.Build();

// MIDDLEWARE
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Proyecto de Clase 847")
            .WithPreferredScheme("Bearer")
            .WithHttpBearerAuthentication(bearer =>
            {
                bearer.Token = "";
            });
    });
}

app.UseMiddleware<ErrorHandlingMiddleware>();

// CORS debe ir antes de Authentication y Authorization
// Las peticiones del frotend se rechazan antes de llegar al auth
app.UseCors("AllowAll");

// lee el token del header y valida quien es el usuario
app.UseAuthentication();

// verifica que el usuario este autenticado y con permisos para el endpoint
app.UseAuthorization();

// conecta las rutas HTTP con los metodos de nuestros controllers
app.MapControllers();

app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200"));

app.Run();