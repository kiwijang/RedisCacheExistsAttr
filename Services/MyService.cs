using System.Text;
using System.Text.Json;
using dooo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace dooo.Services
{
    public interface IMyService
    {
        Task<IEnumerable<string>> GetCountryAsync(string apiKey);
        Task<IEnumerable<string>> GetCacheDataByKey(string apiKey);
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

        public async Task<IEnumerable<string>> GetCountryAsync(string apiKey)
        {
            // 撈 DB
            var result = await this._context.Countries.Select(x => x.Code).ToListAsync();
            // 設定到 redis
            await this._setToRedisAsync(result, apiKey);
            return result;
        }

        /// <summary>
        /// 透過 cacheKey 取得 Redis 快取
        /// </summary>
        /// <param name="cacheKey">Redis key (string)</param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetCacheDataByKey(string cacheKey)
        {
            // Cache key found, short-circuit the action and return cached data
            byte[]? cachedData = await this._cache.GetAsync(cacheKey);
            if (cachedData is null)
            {
                return new List<string>();
            }
            var cachedDataString = Encoding.UTF8.GetString(cachedData);
            var result = JsonSerializer.Deserialize<IEnumerable<string>>(cachedDataString);
            return result ?? new List<string>();
        }

        /// <summary>
        /// 物件轉換成 byte[]
        /// </summary>
        /// <param name="obj">物件，如 List</param>
        /// <returns></returns>
        private byte[] _objTobyteArray(object obj)
        {
            // Serializing the data
            string cachedDataString = JsonSerializer.Serialize(obj);
            byte[] dataToCache = Encoding.UTF8.GetBytes(cachedDataString);
            return dataToCache;
        }

        /// <summary>
        /// 設定 redis key(string)/value(byte[])
        /// </summary>
        /// <param name="obj">物件，如 List</param>
        /// <param name="apiKey">Redis key 這邊請用 controller api 方法命名</param>
        /// <returns></returns>
        private async Task _setToRedisAsync(object obj, string apiKey)
        {
            // 存入 Redis 並設定兩小時過期
            // https://www.c-sharpcorner.com/article/easily-use-redis-cache-in-asp-net-6-0-web-api/
            // Serializing the data
            byte[] dataToCache = this._objTobyteArray(obj);

            // Setting up the cache options
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.Now.AddHours(2)); // 兩小時後過期

            // Add the data into the cache
            await this._cache.SetAsync(apiKey, dataToCache, options);
        }
    }
}