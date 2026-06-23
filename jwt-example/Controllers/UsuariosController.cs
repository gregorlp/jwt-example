using jwt_example.JWT;
using jwt_example.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace jwt_example.Controllers
{
    [ApiController]
    [Route("usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly JwtOptions _jwt;

        public UsuariosController(IOptions<JwtOptions> options)
        {
            _jwt = options.Value;
        }

        [Authorize]
        [HttpGet("test")]
        public IActionResult TestAuthentication()
        {
            return Ok(new
            {
                message = "Valid token",
                claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginViewModel model)
        {
            // NOTE: Credential validation omitted intentionally.
            // In production, validate against your user store here.

            var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwt.SecretKey));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,

                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "123"),
                    new Claim(JwtRegisteredClaimNames.Email, "user@test.com"),
                    new Claim("role", "Admin")
                }),

                Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes),

                SigningCredentials = new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                )
            };

            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(descriptor);

            return Ok(new LoginResponse() { Token = token });
        }
    }
}
