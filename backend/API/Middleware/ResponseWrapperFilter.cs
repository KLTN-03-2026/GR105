using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using backend.Application.DTOs;
using System.Net;

namespace backend.API.Middleware
{
    public class ResponseWrapperFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? (int)HttpStatusCode.OK;

                // Skip wrapping if it's already an ApiResponse
                if (objectResult.Value != null && objectResult.Value.GetType().IsGenericType &&
                    objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    await next();
                    return;
                }

                if (statusCode >= 200 && statusCode < 300)
                {
                    objectResult.Value = new ApiResponse<object>(objectResult.Value);
                }
                else
                {
                    // For error cases (like BadRequest), wrap in Error response
                    var message = "An error occurred";
                    if (objectResult.Value is ValidationProblemDetails validationProblems)
                    {
                        var errors = validationProblems.Errors.SelectMany(x => x.Value).ToList();
                        objectResult.Value = new ApiResponse<object>("Validation failed", errors);
                    }
                    else
                    {
                        objectResult.Value = new ApiResponse<object>(objectResult.Value?.ToString() ?? message);
                    }
                }
            }
            else if (context.Result is StatusCodeResult statusCodeResult)
            {
                var statusCode = statusCodeResult.StatusCode;
                if (statusCode >= 200 && statusCode < 300)
                {
                    context.Result = new ObjectResult(new ApiResponse<object>(null, "Success")) { StatusCode = statusCode };
                }
                else
                {
                    context.Result = new ObjectResult(new ApiResponse<object>($"Status Code: {statusCode}")) { StatusCode = statusCode };
                }
            }

            await next();
        }
    }
}
