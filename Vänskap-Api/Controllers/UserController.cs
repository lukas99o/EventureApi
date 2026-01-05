using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vänskap_Api.Models.Dtos.User;
using Vänskap_Api.Service.IService;

[Authorize(Roles = "Admin,User")]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("upload-profile-picture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureDto dto)
    {
        try
        {
            var profilePicturePath = await _userService.UploadProfilePictureAsync(dto.ProfilePicture);
            return Ok(new { ProfilePicturePath = profilePicturePath });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("delete-profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        try
        {
            var result = await _userService.DeleteProfilePictureAsync();
            if (result)
            {
                return Ok(new { Message = "Profile picture deleted successfully." });
            }
            else
            {
                return BadRequest(new { Error = "Failed to delete profile picture." });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult> GetUser(string userId)
    {
        var user = await _userService.GetUser(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("update-about/{newAboutText}")]
    public async Task<IActionResult> UpdateAbout(string newAboutText)
    {
        try
        {
            var result = await _userService.UpdateUserAbout(newAboutText);
            if (result)
            {
                return Ok(new { Message = "About section updated successfully." });
            }
            else
            {
                return BadRequest(new { Error = "Failed to update About section." });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}