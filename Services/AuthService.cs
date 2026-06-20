using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;

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

        var existing = await collection
            .WhereEqualTo("Email", dto.Email)
            .GetSnapshotAsync();

        if (existing.Count > 0)
            throw new Exception("Ya existe un usuario con ese correo");

        List<string> rolesValidos;

        if (dto.Roles.Contains("admin"))
        {
            rolesValidos = new List<string> { "admin" };
        }
        else
        {
            var rolesPermitidos = new[] { "donor", "receiver" };
            rolesValidos = dto.Roles
                .Where(r => rolesPermitidos.Contains(r))
                .ToList();

            if (rolesValidos.Count == 0)
                rolesValidos.Add("receiver");
        }

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

        if (data.ContainsKey("IsActive") && !(bool)data["IsActive"])
            throw new Exception("Esta cuenta está desactivada");

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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
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
        if (dto.Roles != null && dto.Roles.Count > 0) updates["Roles"] = dto.Roles;
        if (updates.Count > 0)
            await _firebaseService.GetCollection("users").Document(userId).UpdateAsync(updates);
    }

    public async Task<object> GenerateNewTokenAsync(string userId)
    {
        var user = await GetById(userId)
            ?? throw new KeyNotFoundException("Usuario no encontrado");

        var refreshToken = await CreateRefreshTokenAsync(userId);
        var jwt = GenerateToken(user);

        return new
        {
            Token = jwt,
            RefreshToken = refreshToken,
            User = new { user.Id, user.FullName, user.Email, user.Zone, user.Roles }
        };
    }

    // ─── REFRESH TOKEN ────────────────────────────────────────────────

    public async Task<object> RefreshTokenAsync(string refreshToken)
    {
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
            RandomNumberGenerator.GetBytes(64));

        await _firebaseService.GetCollection("refreshTokens")
            .Document(Guid.NewGuid().ToString())
            .SetAsync(new Dictionary<string, object>
            {
                { "Token", token },
                { "UserId", userId },
                { "IsRevoked", false },
                { "CreatedAt", DateTime.UtcNow },
                { "ExpiresAt", DateTime.UtcNow.AddDays(7) }
            });

        return token;
    }

    // ─── RECUPERACIÓN DE CONTRASEÑA ──────────────────────────────────

    public async Task ForgotPasswordAsync(string email)
    {
        var snap = await _firebaseService.GetCollection("users")
            .WhereEqualTo("Email", email)
            .GetSnapshotAsync();

        if (snap.Count == 0) return;

        var userId = snap.Documents[0].ToDictionary()["Id"].ToString()!;
        var fullName = snap.Documents[0].ToDictionary()["FullName"].ToString()!;

        var resetToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

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

        try
        {
            await SendResetEmailAsync(email, fullName, resetToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando email: {ex.Message}");
        }
    }

    private async Task SendResetEmailAsync(string email, string fullName, string resetToken)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);

        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetUrl = $"http://localhost:4200/reset-password?token={encodedToken}&email={encodedEmail}";

        var msg = new SendGridMessage
        {
            From = new EmailAddress(
                _configuration["SendGrid:FromEmail"],
                _configuration["SendGrid:FromName"]
            ),
            Subject = "Restablecer tu contraseña - DonaCerca",
            HtmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #c2185b;'>🤝 DonaCerca</h2>
                    <p>Hola <strong>{fullName}</strong>,</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña.</p>
                    <p>Haz clic en el siguiente botón para continuar:</p>
                    <a href='{resetUrl}'
                       style='background-color: #c2185b; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; display: inline-block;'>
                        Restablecer contraseña
                    </a>
                    <p style='margin-top: 20px; color: #666;'>
                        Este enlace expira en <strong>1 hora</strong>.
                    </p>
                    <p style='color: #666;'>
                        Si no solicitaste esto, ignora este correo.
                    </p>
                </div>"
        };

        msg.AddTo(new EmailAddress(email, fullName));

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Error enviando email: {body}");
        }
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

        await _firebaseService.GetCollection("users")
            .Document(userId)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "PasswordHash", HashPassword(dto.NewPassword) }
            });

        await snap.Documents[0].Reference.UpdateAsync(
            new Dictionary<string, object> { { "Used", true } });

        await RevokeRefreshTokenAsync(userId);
    }

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
            ExpiresAt = ((Timestamp)data["ExpiresAt"]).ToDateTime()
        };
    }

    // ─── GOOGLE LOGIN ─────────────────────────────────────────────────

    public async Task<object> GoogleLoginAsync(string idToken)
    {
        var decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance
            .VerifyIdTokenAsync(idToken);

        var uid = decodedToken.Uid;
        var email = decodedToken.Claims["email"].ToString()!;
        var name = decodedToken.Claims.ContainsKey("name")
            ? decodedToken.Claims["name"].ToString()!
            : email.Split('@')[0];

        var collection = _firebaseService.GetCollection("users");
        var existing = await collection.WhereEqualTo("Email", email).GetSnapshotAsync();

        User user;

        if (existing.Count > 0)
        {
            var d = existing.Documents[0].ToDictionary();
            user = new User
            {
                Id = d["Id"].ToString()!,
                FullName = d["FullName"].ToString()!,
                Email = d["Email"].ToString()!,
                Zone = d.ContainsKey("Zone") ? d["Zone"].ToString()! : "",
                Roles = ((List<object>)d["Roles"]).Select(r => r.ToString()!).ToList(),
                IsActive = (bool)d["IsActive"]
            };
        }
        else
        {
            user = new User
            {
                Id = uid,
                FullName = name,
                Email = email,
                PasswordHash = "",
                Zone = "",
                Roles = new List<string> { "receiver" },
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
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Esta cuenta está desactivada");

        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        var jwt = GenerateToken(user);

        return new
        {
            Token = jwt,
            RefreshToken = refreshToken,
            IsNewUser = existing.Count == 0,
            User = new { user.Id, user.FullName, user.Email, user.Zone, user.Roles }
        };
    }
}