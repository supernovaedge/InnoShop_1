using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UserManagement.Application.DTOs;
using UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace UserManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;
        private readonly ProblemDetailsFactory _problemDetailsFactory;

        public LoginController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, ILogger<LoginController> logger, ProblemDetailsFactory problemDetailsFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _problemDetailsFactory = problemDetailsFactory;
        }

        [HttpPost]
        [Route("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            _logger.LogInformation("Attempting to find user by email: {Email}", loginDto.Email);
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", loginDto.Email);
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Invalid email or password"));
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning("Email not confirmed for user: {UserId}", user.Id);
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Email not confirmed"));
            }

            _logger.LogInformation("User found: {UserId}", user.Id);
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid password for user: {UserId}", user.Id);
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Invalid email or password"));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };
            claims.AddRange(roleClaims);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.GetValue<string>("Issuer"),
                audience: jwtSettings.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


