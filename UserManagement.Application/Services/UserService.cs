using AutoMapper;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Core.Interfaces;
using UserManagement.Domain.Entities;
using ProductManagement.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IProductRepository productRepository, IMapper mapper, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _logger = logger;
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

                if (wasActive && !user.IsActive)
                {
                    _logger.LogInformation($"Soft deleting products for user {user.Id}");
                    await _productRepository.SoftDeleteByUserIdAsync(user.Id);
                }
                else if (!wasActive && user.IsActive)
                {
                    _logger.LogInformation($"Restoring products for user {user.Id}");
                    await _productRepository.RestoreByUserIdAsync(user.Id);
                }
            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}
