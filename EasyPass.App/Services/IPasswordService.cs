using EasyPass.App.Models;

namespace EasyPass.App.Services
{
    // Interface for all password-related API operations.
    public interface IPasswordService
    {
        Task<List<PasswordEntry>> GetAllPasswordsAsync();
        Task<bool> CreatePasswordAsync(PasswordEntry newPassword);
        Task<bool> UpdatePasswordAsync(int id, PasswordEntry updatedPassword);
        Task<bool> DeletePasswordAsync(int id);
        Task<string> GeneratePasswordAsync();
    }
}
