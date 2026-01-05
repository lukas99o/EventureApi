using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.User;
using Vänskap_Api.Service.IService;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _contextAccessor;
    private string UserId => _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentNullException(nameof(UserId));


    public UserService(UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IHttpContextAccessor contextAccessor)
    {
        _userManager = userManager;
        _environment = environment;
        _contextAccessor = contextAccessor;
    }

    public async Task<UserDto?> GetUser(string userId)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                UserName = u.UserName!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Age = DateTime.Now.Year - u.DateOfBirth.Year - (DateTime.Now.DayOfYear < u.DateOfBirth.DayOfYear ? 1 : 0),
                ProfilePicturePath = u.ProfilePicturePath ?? string.Empty,
                About = u.About ?? string.Empty
            })
            .FirstOrDefaultAsync();

        return user; 
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile profilePicture)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new Exception("Invalid file type");
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
        Directory.CreateDirectory(uploadsFolder); 
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(fileStream);
        }

        user.ProfilePicturePath = $"/images/profiles/{uniqueFileName}";
        await _userManager.UpdateAsync(user);

        return user.ProfilePicturePath;
    }

    public async Task<bool> UpdateUserAbout(string newAboutText)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        user.About = newAboutText;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteProfilePictureAsync()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        if (string.IsNullOrEmpty(user.ProfilePicturePath))
        {
            throw new Exception("No profile picture to delete");
        }

        var filePath = Path.Combine(_environment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        user.ProfilePicturePath = null;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}