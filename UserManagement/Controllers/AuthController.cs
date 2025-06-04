using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Features;

namespace UserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto request)
        {
            try
            {
                var command = new LogInUser.Command(request);
                var result = await mediator.Send(command);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante login");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
