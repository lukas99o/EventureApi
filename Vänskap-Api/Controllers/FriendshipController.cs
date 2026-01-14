using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vänskap_Api.Models;
using Vänskap_Api.Models.Dtos.Friend;
using Vänskap_Api.Models.Dtos.User;
using Vänskap_Api.Service.IService;

namespace Vänskap_Api.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        [HttpPost("send-friend-request/{username}")]
        public async Task<IActionResult> SendFriendRequest(string username)
        {
            var result = await _friendshipService.SendFriendRequest(username);
            if (!result) return BadRequest($"Could not find user with username: {username}.");

            return Ok();
        }

        [HttpPost("accept-friend-request/{username}")]
        public async Task<IActionResult> AcceptFriendRequest(string username)
        {
            var result = await _friendshipService.AcceptFriendRequest(username);
            if (!result) return BadRequest("No friend request with the given username.");

            return Ok("Accepted friend request.");
        }

        [HttpGet("friends")]
        public async Task<ActionResult<IEnumerable<string>>> SeeFriendList()
        {
            return Ok(await _friendshipService.SeeFriendList());
        }

        [HttpGet("friend-requests")]
        public async Task<ActionResult<GetFriendRequestsDto>> SeeFriendRequests()
        {
            return Ok(await _friendshipService.SeeFriendRequests());
        }

        [HttpDelete("removefriend/{userName}")]
        public async Task<IActionResult> RemoveFriend(string userName)
        {
            var result = await _friendshipService.RemoveFriend(userName);
            if (!result) return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("decline-friend-request/{username}")]
        public async Task<IActionResult> DeclineFriendRequest(string username)
        {
            var result = await _friendshipService.DeclineFriendRequest(username);
            if (!result) return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("regret-friend-request/{username}")]
        public async Task<IActionResult> RemoveFriendRequest(string username)
        {
            var result = await _friendshipService.RemoveFriendRequest(username);
            if (!result) return BadRequest();

            return Ok(result);
        }
    }
}
