using UserManagement.Application.DTOs;
using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task AddUserAsync(CreateUserDto createUserDto);
        Task UpdateUserAsync(UpdateUserDto updateUserDto);
        Task DeleteUserAsync(Guid id);
    }
}


