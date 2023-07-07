using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace MyAttributes
{
    public class RedisCacheExistsAttribute : ActionFilterAttribute
    {
        private string _cacheKey;

        public RedisCacheExistsAttribute()
        {
            _cacheKey = string.Empty; // 初始值设为空字符串
        }

        public string CacheKey
        {
            get { return _cacheKey; }
            set { _cacheKey = value; }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var cache = serviceProvider.GetService<IDistributedCache>();

            if (cache is null) return;
            if (await cache.GetAsync(_cacheKey) is null)
            {
                // Cache key not found, continue with the action execution
                await next();
            }
            else
            {
                // Cache key found, short-circuit the action and return cached data
                byte[]? cachedData = await cache.GetAsync(_cacheKey);
                if (cachedData is not null)
                {
                    var cachedDataString = Encoding.UTF8.GetString(cachedData);
                    var result = JsonSerializer.Deserialize<IEnumerable<string>>(cachedDataString);
                    context.Result = new OkObjectResult(result ?? new List<string>());
                    return;
                }
            }
        }
    }
}
