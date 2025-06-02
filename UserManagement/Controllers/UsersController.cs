using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Features;

namespace UserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(IMediator mediator) : ControllerBase
    {
        /// <summary>
        /// Creates a new User record.
        /// </summary>
        [HttpPost(Name = "AddUser")]
        public async Task<ActionResult<UserDto>> AddUser([FromBody] UserForCreationDto userForCreation)
        {
            var command = new AddUser.Command(userForCreation);
            var commandResponse = await mediator.Send(command);

            return Ok();
        }

        /// <summary>
        /// Gets a list of all Users.
        /// </summary>
        [HttpGet(Name = "GetUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] UserParametersDto userParametersDto)
        {
            var query = new GetUserList.Query(userParametersDto);
            var queryResponse = await mediator.Send(query);

            return Ok(queryResponse);
        }

        /// <summary>
        /// Updates an entire existing User.
        /// </summary>
        [HttpPut("{userId:guid}", Name = "UpdateUser")]
        public async Task<IActionResult> UpdateUser(Guid userId, UserForUpdateDto user)
        {
            var command = new UpdateUser.Command(userId, user);
            await mediator.Send(command);
            return Ok();
        }
    }
}
