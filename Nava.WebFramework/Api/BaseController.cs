using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nava.WebFramework.Filters;

namespace Nava.WebFramework.Api
{
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]// api/v1/[controller]
    //[Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
        //public UserRepository UserRepository { get; set; } => property injection
        //public bool UserIsAuthenticated => HttpContext.User.Identity.IsAuthenticated;
    }
}
