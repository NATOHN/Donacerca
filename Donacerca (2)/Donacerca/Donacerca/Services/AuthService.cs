using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;
using Microsoft.IdentityModel.Tokens;

namespace Donacerca.Services;

public class AuthService
{
    private readonly FirebaseService _firebaseService;
    private readonly IConfiguration _configuration;

    public AuthService(FirebaseService firebaseService, IConfiguration configuration)
    {
        _firebaseService = firebaseService;
        _configuration = configuration;
    }

    public async Task<User> Register(RegisterDto dto)
    {
        var collection = _firebaseService.GetCollection("users");

        // Verificar email duplicado
        var existing = await collection
            .WhereEqualTo("Email", dto.Email)
            .GetSnapshotAsync();

        if (existing.Count > 0)
            throw new Exception("Ya existe un usuario con ese correo");

        // Validar roles permitidos
        var rolesPermitidos = new[] { "donor", "receiver" };
        var rolesValidos = dto.Roles
            .Where(r => rolesPermitidos.Contains(r))
            .ToList();

        if (rolesValidos.Count == 0)
            rolesValidos.Add("receiver");

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Zone = dto.Zone,
            Roles = rolesValidos,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await collection.Document(user.Id).SetAsync(new Dictionary<string, object>
        {
            { "Id", user.Id },
            { "FullName", user.FullName },
            { "Email", user.Email },
            { "PasswordHash", user.PasswordHash },
            { "Zone", user.Zone },
            { "Roles", user.Roles },
            { "IsActive", user.IsActive },
            { "CreatedAt", user.CreatedAt }
        });

        return user;
    }

    public async Task<object> Login(LoginDto dto)
    {
        var collection = _firebaseService.GetCollection("users");
        var snapshot = await collection
            .WhereEqualTo("Email", dto.Email)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
            throw new Exception("No existe ningún usuario con esas credenciales");

        var doc = snapshot.Documents[0];
        var data = doc.ToDictionary();

        // Verificar que la cuenta esté activa
        if (data.ContainsKey("IsActive") && !(bool)data["IsActive"])
            throw new Exception("Esta cuenta está desactivada");

        // Mapear roles (Firestore los devuelve como List<object>)
        var roles = data.ContainsKey("Roles")
            ? ((List<object>)data["Roles"]).Select(r => r.ToString()!).ToList()
            : new List<string> { "receiver" };

        var user = new User
        {
            Id = data["Id"].ToString()!,
            FullName = data["FullName"].ToString()!,
            Email = data["Email"].ToString()!,
            PasswordHash = data["PasswordHash"].ToString()!,
            Zone = data.ContainsKey("Zone") ? data["Zone"].ToString()! : "",
            Roles = roles,
            IsActive = data.ContainsKey("IsActive") && (bool)data["IsActive"],
            CreatedAt = ((Timestamp)data["CreatedAt"]).ToDateTime()
        };

        if (!VerifyPassword(dto.Password, user.PasswordHash))
            throw new Exception("Contraseña incorrecta");

        var token = GenerateToken(user);

        // Devolvemos token + info básica del usuario para que el frontend
        // sepa qué panel mostrar según los roles
        return new
        {
            Token = token,
            User = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Zone,
                user.Roles
            }
        };
    }

    private string GenerateToken(User user)
    {
        // Un Claim por cada rol — así [Authorize(Roles="donor")] funciona correctamente
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
        };

        // Agregar un claim de rol por cada rol del usuario
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"], // ← bug corregido
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}