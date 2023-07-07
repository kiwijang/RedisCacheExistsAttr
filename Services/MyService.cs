using System.Text;
using System.Text.Json;
using dooo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace dooo.Services
{
    public interface IMyService
    {
        //DB
        Task CreateCountryAsync(string apiKey, Country entity);
        Task<IEnumerable<string>> GetCountryAsync(string apiKey);
        Task UpdateCountryAsync(string apiKey, Country entity);
        Task DeleteCountryAsync(string apiKey, string code);

        //redis
        Task DeleteCacheDataByKey(string cacheKey);
        Task<IEnumerable<string>> GetCacheDataByKey(string cacheKey);
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

        public async Task DeleteCountryAsync(string apiKey, string code)
        {
            // 撈 DB
            var parent =
            this._context.Countries.Include(p => p.Cities).Include(p => p.Countrylanguages).FirstOrDefault(p => p.Code == code);
            if (parent is null) return;

            this._context.Cities.RemoveRange(parent.Cities);
            this._context.Countrylanguages.RemoveRange(parent.Countrylanguages);
            this._context.Countries.Remove(parent);
            var result = await this._context.SaveChangesAsync();

            return;
        }

        public async Task UpdateCountryAsync(string apiKey, Country entity)
        {
            var updEntity = await this._context.Countries.FindAsync(entity.Code);
            if (updEntity is null) return;

            updEntity.Capital = entity.Capital;
            updEntity.Code2 = entity.Code2;
            updEntity.Continent = entity.Continent;
            updEntity.Gnp = entity.Gnp;
            updEntity.Gnpold = entity.Gnpold;
            updEntity.GovernmentForm = entity.GovernmentForm;
            updEntity.HeadOfState = entity.HeadOfState;
            updEntity.IndepYear = entity.IndepYear;
            updEntity.LifeExpectancy = entity.LifeExpectancy;
            updEntity.LocalName = entity.LocalName;
            updEntity.Name = entity.Name;
            updEntity.Population = entity.Population;
            updEntity.Region = entity.Region;
            updEntity.SurfaceArea = entity.SurfaceArea;

            this._context.Countries.Update(updEntity);
            // https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=data-annotations#resolving-concurrency-conflicts
            await this._context.SaveChangesAsync();
        }

        public async Task CreateCountryAsync(string apiKey, Country entity)
        {
            var updEntity = new Country();
            updEntity.Capital = entity.Capital;
            updEntity.Code = entity.Code;
            updEntity.Code2 = entity.Code2;
            updEntity.Continent = entity.Continent;
            updEntity.Gnp = entity.Gnp;
            updEntity.Gnpold = entity.Gnpold;
            updEntity.GovernmentForm = entity.GovernmentForm;
            updEntity.HeadOfState = entity.HeadOfState;
            updEntity.IndepYear = entity.IndepYear;
            updEntity.LifeExpectancy = entity.LifeExpectancy;
            updEntity.LocalName = entity.LocalName;
            updEntity.Name = entity.Name;
            updEntity.Population = entity.Population;
            updEntity.Region = entity.Region;
            updEntity.SurfaceArea = entity.SurfaceArea;
            await this._context.Countries.AddAsync(updEntity);
            var result = await this._context.SaveChangesAsync();

            return;
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
        /// 透過 cacheKey 刪除 Redis 緩存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public async Task DeleteCacheDataByKey(string cacheKey)
        {
            await this._cache.RemoveAsync(cacheKey);
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