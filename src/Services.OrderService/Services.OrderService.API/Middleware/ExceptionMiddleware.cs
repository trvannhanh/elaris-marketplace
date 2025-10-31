using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Services.OrderService.API.Middleware
{
    public static class ExceptionMiddleware
    {
        public static void AddGlobalExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = exceptionFeature?.Error;

                    if (exception == null)
                        return;

                    var (status, title) = exception switch
                    {
                        ValidationException => (HttpStatusCode.BadRequest, "Validation Failed"),
                        KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
                        _ => (HttpStatusCode.InternalServerError, "Server Error")
                    };

                    logger.LogError(exception, "Unhandled exception occurred");

                    var problem = new ProblemDetails
                    {
                        Status = (int)status,
                        Title = title,
                        Detail = exception.Message,
                        Instance = context.Request.Path
                    };

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = problem.Status.Value;

                    await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                });
            });
        }
    }
}
