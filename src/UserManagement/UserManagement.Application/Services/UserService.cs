using AutoMapper;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Core.Interfaces;
using UserManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(HttpClient httpClient, IUserRepository userRepository, IMapper mapper, ILogger<UserService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task AddUserAsync(CreateUserDto createUserDto)
        {
            var user = _mapper.Map<User>(createUserDto);
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            await _userRepository.AddAsync(user);
        }

        public async Task UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(updateUserDto.Id);
            if (user != null)
            {
                bool wasActive = user.IsActive;
                _mapper.Map(updateUserDto, user);
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation($"User state changed: WasActive={wasActive}, IsActive={user.IsActive}");

                var bearerToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    if (wasActive && !user.IsActive)
                    {
                        _logger.LogInformation($"Soft deleting products for user {user.Id}");
                        var response = await _httpClient.PostAsync($"/api/products/softdelete/{user.Id}", null);
                        response.EnsureSuccessStatusCode();
                    }
                    else if (!wasActive && user.IsActive)
                    {
                        _logger.LogInformation($"Restoring products for user {user.Id}");
                        var response = await _httpClient.PostAsync($"/api/products/restore/{user.Id}", null);
                        response.EnsureSuccessStatusCode();
                    }
                }
                else
                {
                    _logger.LogWarning("Bearer token is missing from the Authorization header.");
                }
            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}
