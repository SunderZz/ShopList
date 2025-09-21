using Microsoft.AspNetCore.Builder;
using ListeDeCourses.Api.Middlewares;

namespace ListeDeCourses.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseGlobalErrorHandler(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
