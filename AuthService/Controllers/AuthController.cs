using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

//Webapi controller 
[ApiController]
[Route("api/[controller]")] //AuthController -> /api/auth
public class AuthController : ControllerBase
{
    //Private variable storing service object.
    private readonly IAuthService _authService;

    //creates AuthService and injects it
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    /// Register a new user
    [HttpPost("register")] //: got to url post /api/auth/register
    [AllowAnonymous] //No JWT Token Required
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    
    //method for registering the user 
    //says-read data(json) from HTTP req and convert to C# object RRDto
    //[FromBody] : take data from req. body
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        //service call 
        var response = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }


    /// Authenticate user and return JWT token
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response); //returns jwt token with status code
    }


    /// Get details of the currently logged-in user
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    
    //Get info about current logged user
    public async Task<IActionResult> GetCurrentUser()
    {
        //read UserId from JWT token
        //Fallback : If UserId claim is missing, look for: NameIdentifier instead.
        var userIdClaim = User.FindFirst("UserId")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            //bcoz token is broken
            return Unauthorized(new { message = "Invalid or missing user identity in token." });
        }

        //send UserId to Service
        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(user); //sends status code with user-info
    }
}
