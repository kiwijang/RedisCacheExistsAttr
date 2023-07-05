using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using dooo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace dooo.Services
{
    public interface IMyService
    {
        Task<IEnumerable<string>> GetCountryAsync();
    }

    public class MyService : IMyService
    {
        private readonly WorldContext _context;
        private readonly IDistributedCache _cache;

        public MyService(WorldContext context, IDistributedCache cache)
        {
            this._context = context;
            this._cache = cache;
        }

        public async Task<IEnumerable<string>> GetCountryAsync()
        {
            string apiId = "GetCountryAsync";
            if (this._cache.Get(apiId) is null)
            {
                // 撈 DB
                var result = await this._context.Countries.Select(x => x.Code).ToListAsync();
                // 存入 Redis 並設定兩小時過期

                // https://www.c-sharpcorner.com/article/easily-use-redis-cache-in-asp-net-6-0-web-api/
                // Serializing the data
                string cachedDataString = JsonSerializer.Serialize(result);
                var dataToCache = Encoding.UTF8.GetBytes(cachedDataString);

                // Setting up the cache options
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddHours(2)); // 兩小時後過期

                // Add the data into the cache
                await this._cache.SetAsync(apiId, dataToCache, options);
                return result;
            }
            else
            {
                // 撈 Redis
                // If the data is found in the cache, encode and deserialize cached data.
                byte[]? cachedData = await this._cache.GetAsync(apiId);
                if (cachedData is not null)
                {
                    var cachedDataString = Encoding.UTF8.GetString(cachedData);
                    var result = JsonSerializer.Deserialize<IEnumerable<string>>(cachedDataString);
                    return result ?? new List<string>();
                }
            }

            return new List<string>();
        }
    }
}