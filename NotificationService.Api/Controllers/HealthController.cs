using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly INotificationProviderManager _providerManager;

        public HealthController(INotificationProviderManager providerManager)
        {
            _providerManager = providerManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var providerStatus = await _providerManager.CheckAllProvidersHealthAsync();
                var isHealthy = providerStatus.Values.Any(v => v.IsHealthy);

                if (isHealthy)
                {
                    return Ok(new
                    {
                        status = "healthy",
                        providers = providerStatus.ToDictionary(
                            kvp => kvp.Key.ToString(),
                            kvp => new
                            {
                                isHealthy = kvp.Value.IsHealthy,
                                status = kvp.Value.Status,
                                responseTime = kvp.Value.ResponseTime.TotalMilliseconds
                            })
                    });
                }
                else
                {
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        providers = providerStatus.ToDictionary(
                            kvp => kvp.Key.ToString(),
                            kvp => new
                            {
                                isHealthy = kvp.Value.IsHealthy,
                                status = kvp.Value.Status,
                                error = kvp.Value.ErrorMessage
                            })
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
