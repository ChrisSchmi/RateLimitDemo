using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
static string GetUserEndPoint(HttpContext context) => $"User {context.User.Identity?.Name ?? "Anonymous"} endpoint:{context.Request.Path}" + $" {context.Connection.RemoteIpAddress}";


// See:
// https://www.milanjovanovic.tech/blog/advanced-rate-limiting-use-cases-in-dotnet
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("fixed-by-ip", httpContext =>
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();

        var limiter = RateLimitPartition.GetFixedWindowLimiter(forwardedFor,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 4,
                    Window = TimeSpan.FromMinutes(1),
                });

        return limiter;
    });

    // see:
    // https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
    options.OnRejected =  (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.RequestServices.GetService<ILoggerFactory>() ? 
            .CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware")
            .LogWarning("OnRejected: {GetUserEndPoint}", GetUserEndPoint(context.HttpContext));

        if (context.HttpContext.Response.Body.CanWrite == true)
        {
            var tooMany = "you sent too many requests!";
            var bytes = Encoding.Default.GetBytes(tooMany);
            var t = context.HttpContext.Response.Body.WriteAsync(bytes, 0, tooMany.Length);

            Task.WaitAll(t);

        }
        return new ValueTask();
    };
});



//builder.Services.AddRateLimiter(_ => _
//    .AddFixedWindowLimiter(policyName: "fixed", options =>
//    {
//        options.PermitLimit = 4;
//        options.Window = TimeSpan.FromSeconds(60);
//        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//        options.QueueLimit = 2;
//    }));



builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Enable Rate Limiter
app.UseRateLimiter();

app.Run();
