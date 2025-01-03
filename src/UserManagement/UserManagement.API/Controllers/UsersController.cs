using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Domain.Entities;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace UserManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ProblemDetailsFactory _problemDetailsFactory;

        public UsersController(IUserService userService, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailSender emailSender, ProblemDetailsFactory problemDetailsFactory)
        {
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _problemDetailsFactory = problemDetailsFactory;
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "User not found"));
            }
            return Ok(user);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            var roleExists = await _roleManager.RoleExistsAsync(createUserDto.Role);
            if (!roleExists)
            {
                return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, $"Role {createUserDto.Role} does not exist."));
            }

            var user = new User
            {
                UserName = createUserDto.Email,
                Email = createUserDto.Email,
                Name = createUserDto.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, createUserDto.Role);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Users", new { userId = user.Id, token }, Request.Scheme);

            var emailBody = $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(confirmationLink ?? string.Empty)}'>link</a>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", emailBody);

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (id != updateUserDto.Id)
            {
                return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, "User ID mismatch"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            await _userService.UpdateUserAsync(updateUserDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _userService.DeleteUserAsync(id);
            return NoContent();
        }

        [HttpGet("ConfirmEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, "Invalid user ID or token"));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "User not found"));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok("Email confirmed successfully!");
            }

            return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, "Error confirming email"));
        }

        [HttpPost("RequestPasswordReset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto passwordResetRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(passwordResetRequestDto.Email);
            }
            catch (FormatException)
            {
                return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, "Invalid email format"));
            }

            var user = await _userManager.FindByEmailAsync(passwordResetRequestDto.Email);
            if (user == null)
            {
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "User not found"));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(nameof(ResetPassword), "Users", new { token, email = user.Email }, Request.Scheme);

            var emailBody = $"Please reset your password by clicking this link: <a href='{HtmlEncoder.Default.Encode(resetLink)}'>link</a>{HtmlEncoder.Default.Encode(token)}";

            await _emailSender.SendEmailAsync(user.Email, "Reset your password", emailBody);

            return Ok("Password reset link has been sent to your email.");
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "User not found"));
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            if (result.Succeeded)
            {
                return Ok("Password has been reset successfully.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }
    }
}
