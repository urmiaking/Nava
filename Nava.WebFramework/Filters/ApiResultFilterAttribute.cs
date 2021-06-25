using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nava.Common;
using Nava.WebFramework.Api;

namespace Nava.WebFramework.Filters
{
    public class ApiResultFilterAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            switch (context.Result)
            {
                case OkObjectResult okObjectResult:
                {
                    var apiResult = new ApiResult<object>(true, ApiResultStatusCode.Success, okObjectResult.Value);
                    context.Result = new JsonResult(apiResult) { StatusCode = okObjectResult.StatusCode };
                    break;
                }
                case OkResult okResult:
                {
                    var apiResult = new ApiResult(true, ApiResultStatusCode.Success);
                    context.Result = new JsonResult(apiResult) { StatusCode = okResult.StatusCode };
                    break;
                }
                case ObjectResult badRequestObjectResult when badRequestObjectResult.StatusCode == 400:
                {
                    string message = null;
                    switch (badRequestObjectResult.Value)
                    {
                        case ValidationProblemDetails validationProblemDetails:
                            var errorMessages = validationProblemDetails.Errors
                                .SelectMany(p => p.Value).Distinct();
                            message = string.Join(" | ", errorMessages);
                            break;
                        case SerializableError errors:
                            var errorMessages2 = errors
                                .SelectMany(p => (string[])p.Value)
                                .Distinct();
                            message = string.Join(" | ", errorMessages2);
                            break;
                        case var value when value != null && !(value is ProblemDetails):
                            message = badRequestObjectResult.Value.ToString();
                            break;
                    }

                    var apiResult = new ApiResult(false, ApiResultStatusCode.BadRequest, message);
                    context.Result = new JsonResult(apiResult) { StatusCode = badRequestObjectResult.StatusCode };
                    break;
                }
                case ObjectResult notFoundObjectResult when notFoundObjectResult.StatusCode == 404:
                {
                    string message = null;
                    if (notFoundObjectResult.Value != null && !(notFoundObjectResult.Value is ProblemDetails))
                        message = notFoundObjectResult.Value.ToString();

                    var apiResult = new ApiResult(false, ApiResultStatusCode.NotFound, message);
                    context.Result = new JsonResult(apiResult) { StatusCode = notFoundObjectResult.StatusCode };
                    break;
                }
                case ContentResult contentResult:
                {
                    var apiResult = new ApiResult(true, ApiResultStatusCode.Success, contentResult.Content);
                    context.Result = new JsonResult(apiResult) { StatusCode = contentResult.StatusCode };
                    break;
                }
                case UnauthorizedResult unauthorizedResult:
                {
                    var apiResult = new ApiResult(false, ApiResultStatusCode.UnAuthorized);
                    context.Result = new JsonResult(apiResult) { StatusCode = unauthorizedResult.StatusCode };
                    break;
                }
                case NoContentResult noContentResult:
                {
                    var apiResult = new ApiResult(true, ApiResultStatusCode.ListEmpty);
                    context.Result = new JsonResult(apiResult) { StatusCode = noContentResult.StatusCode };
                    break;
                }
                case ObjectResult objectResult when objectResult.StatusCode == null && !(objectResult.Value is ApiResult):
                {
                    var apiResult = new ApiResult<object>(true, ApiResultStatusCode.Success, objectResult.Value);
                    context.Result = new JsonResult(apiResult) { StatusCode = objectResult.StatusCode };
                    break;
                }
            }

            base.OnResultExecuting(context);
        }
    }
}