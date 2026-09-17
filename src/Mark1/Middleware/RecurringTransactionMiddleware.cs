using Mark1.Services;

namespace Mark1.Middleware
{
    /// <summary>
    /// Lazily generates due recurring transactions on page load instead of running a background timer.
    /// Throttled process-wide to once a minute since this runs on every request.
    /// </summary>
    public class RecurringTransactionMiddleware
    {
        private static DateTime _lastRunUtc = DateTime.MinValue;
        private static readonly object Lock = new();
        private static readonly TimeSpan Throttle = TimeSpan.FromMinutes(1);

        private readonly RequestDelegate _next;

        public RecurringTransactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRecurringTransactionService recurringService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var shouldRun = false;
                lock (Lock)
                {
                    if (DateTime.UtcNow - _lastRunUtc >= Throttle)
                    {
                        _lastRunUtc = DateTime.UtcNow;
                        shouldRun = true;
                    }
                }

                if (shouldRun)
                {
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await recurringService.ProcessDueRecurringAsync(userId);
                    }
                }
            }

            await _next(context);
        }
    }
}
