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
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        var jwtToken = GenerateToken(user);

        return new
        {
            Token = jwtToken,
            RefreshToken = refreshToken,
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
    
    public async Task<User?> GetById(string id)
    {
        var doc = await _firebaseService.GetCollection("users").Document(id).GetSnapshotAsync();
        if (!doc.Exists) return null;
        var d = doc.ToDictionary();
        return new User
        {
            Id = d["Id"].ToString()!,
            FullName = d["FullName"].ToString()!,
            Email = d["Email"].ToString()!,
            Zone = d.ContainsKey("Zone") ? d["Zone"].ToString()! : "",
            Roles = ((List<object>)d["Roles"]).Select(r => r.ToString()!).ToList()
        };
    }

    public async Task UpdateProfile(string userId, UpdateProfileDto dto)
    {
        var updates = new Dictionary<string, object>();
        if (dto.FullName != null) updates["FullName"] = dto.FullName;
        if (dto.Zone != null) updates["Zone"] = dto.Zone;
        if (updates.Count > 0)
            await _firebaseService.GetCollection("users").Document(userId).UpdateAsync(updates);
    }
    
    // ─── REFRESH TOKEN ────────────────────────────────────────────────

public async Task<object> RefreshTokenAsync(string refreshToken)
{
    // Buscar el refresh token en Firestore
    var snap = await _firebaseService.GetCollection("refreshTokens")
        .WhereEqualTo("Token", refreshToken)
        .WhereEqualTo("IsRevoked", false)
        .GetSnapshotAsync();

    if (snap.Count == 0)
        throw new UnauthorizedAccessException("Refresh token inválido o expirado");

    var data = snap.Documents[0].ToDictionary();
    var expiresAt = ((Timestamp)data["ExpiresAt"]).ToDateTime();

    if (expiresAt < DateTime.UtcNow)
        throw new UnauthorizedAccessException("Refresh token expirado, inicia sesión nuevamente");

    var userId = data["UserId"].ToString()!;
    var user = await GetById(userId)
        ?? throw new KeyNotFoundException("Usuario no encontrado");

    // Revocar el refresh token usado y generar uno nuevo
    await snap.Documents[0].Reference.UpdateAsync(
        new Dictionary<string, object> { { "IsRevoked", true } });

    var newRefreshToken = await CreateRefreshTokenAsync(userId);
    var newJwt = GenerateToken(user);

    return new
    {
        Token = newJwt,
        RefreshToken = newRefreshToken,
        User = new { user.Id, user.FullName, user.Email, user.Zone, user.Roles }
    };
}

public async Task RevokeRefreshTokenAsync(string userId)
{
    // Revocar todos los refresh tokens del usuario (logout)
    var snap = await _firebaseService.GetCollection("refreshTokens")
        .WhereEqualTo("UserId", userId)
        .WhereEqualTo("IsRevoked", false)
        .GetSnapshotAsync();

    foreach (var doc in snap.Documents)
        await doc.Reference.UpdateAsync(
            new Dictionary<string, object> { { "IsRevoked", true } });
}

private async Task<string> CreateRefreshTokenAsync(string userId)
{
    var token = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

    await _firebaseService.GetCollection("refreshTokens")
        .Document(Guid.NewGuid().ToString())
        .SetAsync(new Dictionary<string, object>
        {
            { "Token", token },
            { "UserId", userId },
            { "IsRevoked", false },
            { "CreatedAt", DateTime.UtcNow },
            // Refresh token dura 7 días
            { "ExpiresAt", DateTime.UtcNow.AddDays(7) }
        });

    return token;
}

// ─── LOGIN ACTUALIZADO (devuelve refresh token también) ──────────

// Reemplaza el return al final de tu Login() existente con esto:
// var newRefreshToken = await CreateRefreshTokenAsync(user.Id);
// return new { Token = token, RefreshToken = newRefreshToken, User = new {...} };

// ─── RECUPERACIÓN DE CONTRASEÑA ──────────────────────────────────

public async Task ForgotPasswordAsync(string email)
{
    var snap = await _firebaseService.GetCollection("users")
        .WhereEqualTo("Email", email)
        .GetSnapshotAsync();

    // Siempre respondemos igual aunque no exista (seguridad)
    if (snap.Count == 0) return;

    var userId = snap.Documents[0].ToDictionary()["Id"].ToString()!;

    // Generar token de reset (válido 1 hora)
    var resetToken = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    await _firebaseService.GetCollection("passwordResets")
        .Document(Guid.NewGuid().ToString())
        .SetAsync(new Dictionary<string, object>
        {
            { "Token", resetToken },
            { "Email", email },
            { "UserId", userId },
            { "Used", false },
            { "ExpiresAt", DateTime.UtcNow.AddHours(1) },
            { "CreatedAt", DateTime.UtcNow }
        });

    // En producción aquí enviarías un email con el token
    // Por ahora lo guardamos en Firestore y el admin puede verlo
    // o el frontend lo muestra directamente (modo demo)
}

public async Task ResetPasswordAsync(ResetPasswordDto dto)
{
    var snap = await _firebaseService.GetCollection("passwordResets")
        .WhereEqualTo("Token", dto.Token)
        .WhereEqualTo("Email", dto.Email)
        .WhereEqualTo("Used", false)
        .GetSnapshotAsync();

    if (snap.Count == 0)
        throw new InvalidOperationException("Token inválido o ya utilizado");

    var data = snap.Documents[0].ToDictionary();
    var expiresAt = ((Timestamp)data["ExpiresAt"]).ToDateTime();

    if (expiresAt < DateTime.UtcNow)
        throw new InvalidOperationException("El token ha expirado, solicita uno nuevo");

    var userId = data["UserId"].ToString()!;

    // Actualizar contraseña en Firestore
    await _firebaseService.GetCollection("users")
        .Document(userId)
        .UpdateAsync(new Dictionary<string, object>
        {
            { "PasswordHash", HashPassword(dto.NewPassword) }
        });

    // Marcar token como usado
    await snap.Documents[0].Reference.UpdateAsync(
        new Dictionary<string, object> { { "Used", true } });

    // Revocar todos los refresh tokens por seguridad
    await RevokeRefreshTokenAsync(userId);
}

// Solo para demo — muestra el token de reset en pantalla
public async Task<object> GetResetTokenForDemo(string email)
{
    var snap = await _firebaseService.GetCollection("passwordResets")
        .WhereEqualTo("Email", email)
        .WhereEqualTo("Used", false)
        .GetSnapshotAsync();

    if (snap.Count == 0)
        return new { message = "No hay tokens pendientes para este email" };

    var data = snap.Documents[0].ToDictionary();
    return new
    {
        Token = data["Token"].ToString(),
        Email = data["Email"].ToString(),
        ExpiresAt = ((Google.Cloud.Firestore.Timestamp)data["ExpiresAt"]).ToDateTime()
    };
}
}