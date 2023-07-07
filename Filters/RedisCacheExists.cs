using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace MyAttributes
{
    public class RedisCacheExistsAttribute : ActionFilterAttribute
    {
        private string _cacheKey;

        public RedisCacheExistsAttribute()
        {
            _cacheKey = string.Empty; // 初始值設為空字符串
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
                context.HttpContext.Items["IsRedisKeyExists"] = true;
                await next();
            }
        }
    }
}
