using Vänskap_Api.Models.Dtos.User;

namespace Vänskap_Api.Service.IService
{
    public interface IUserService
    {
        Task<string> UploadProfilePictureAsync(IFormFile profilePicture);
        Task<UserDto?> GetUser(string userId);
        Task<bool> UpdateUserAbout(string newAboutText);
        Task<bool> DeleteProfilePictureAsync();
    }
}
