using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;

namespace Planillas.Application.Features.Auth;

public record LoginRequest(string Email, string Password);
public record UsuarioDto(int Id, string Email, string NombreCompleto, string Rol, int? EmpleadoId);
public record LoginResponse(string Token, DateTime ExpiraEn, UsuarioDto Usuario);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class AuthService(
    IAppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    ICurrentUser currentUser,
    IValidator<LoginRequest> validator)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mismo mensaje para "no existe" y "contraseña incorrecta": no revelamos qué emails están registrados.
        if (usuario is null || !usuario.Activo || !hasher.Verify(request.Password, usuario.PasswordHash))
            throw new UnauthorizedException();

        var (token, expira) = tokens.CrearToken(usuario);
        return new LoginResponse(token, expira, ToDto(usuario));
    }

    public async Task<UsuarioDto> MeAsync(CancellationToken ct)
    {
        var usuario = await db.Usuarios.FindAsync([currentUser.UserId], ct)
            ?? throw new NotFoundException("Usuario", currentUser.UserId);
        return ToDto(usuario);
    }

    private static UsuarioDto ToDto(Domain.Entities.Usuario u) =>
        new(u.Id, u.Email, u.NombreCompleto, u.Rol.ToString(), u.EmpleadoId);
}
