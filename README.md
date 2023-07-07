# RedisCacheExistsAttr
RedisCacheExistsAttribute 實作，與套用到 controller 上，如果存在就撈 Redis/不存在就撈 DB 並存一份到 Redis。

# 使用場景

有些資料的變更頻率不高且存取頻率高，為了重複資料如此高頻率的撈 DB 有點浪費資源，於是使用緩存，將不常變更的重複資料放一份到緩存中，減輕 DB 的壓力。
這邊使用 Redis 實作緩存，最後佈署時還可以將緩存架在與網頁伺服器不同的機器上，萬一機器壞了，緩存還會繼續存在。(不過這個練習都是在本機端(同台機器上))

## redis key 命名方式

是以 Service 名稱為 key 值。(因為正常一個 Controller 只會有一個 Service)

## 為什麼 Redis 緩存設定兩小時過期

這邊考量是萬一更新資料庫的人沒有管理 Redis 的權限，更新 DB 資料後 Redis 刪除 key 的工作沒有提供 API，所以設定兩小時過期可以避免無法刪除緩存的情況發生。
但正常一點的使用方式應該要是不能直接透過 DB 新增資料，規定一定透過 API 來執行 CRUD 的動作:
1. Create(此範例還沒實作)
新增 DB 該資料表項目的 API。(新增後要刪掉 GetCountryAsync 的 redis key/value)。

2. Read
取得 DB 該資料表項目的 API。透過 Attribute 檢查是否已有緩存(如下 redis 緩存狀態圖示)，若有則到 Atrribute 內執行從 Redis 取值的動作，若無則新增 GetCountryAsync 的 redis key/value。

3. Update(此範例還沒實作)
更新 DB 該資料表項目的 API。(更新後要刪掉 GetCountryAsync 的 redis key/value)。

4. Delete(此範例還沒實作)
刪除 DB 該資料表項目的 API。(刪除後要刪掉 GetCountryAsync 的 redis key/value)。

### CUD 三個都要刪掉 GetCountryAsync 的 redis key/value

檢查此 key 是否存在都可用這個 `RedisCacheExistsAttribute`，若存在則用 DeleteCacheDataByKey Service 刪除 GetCountryAsync 的 redis key/value。

`Filters/ RedisCacheExists.cs`
``` c#
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
```

Controller 設定可以參考這個地方
`Controllers/CountryController.cs`
``` C#

    [HttpGet(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<IEnumerable<string>> Get()
    {
        var cacheKey = "GetCountryAsync";
        // 快取存在
        if ((bool)(HttpContext.Items["IsRedisKeyExists"] ?? false))
        {
            return await this._myService.GetCacheDataByKey(cacheKey);
        }
        // 快取不存在
        return await this._myService.GetCountryAsync(cacheKey);
    }
```

# redis 緩存狀態圖
![image](https://github.com/kiwijang/RedisCacheExistsAttr/assets/21300139/750c1235-9b6e-4d6e-a521-493e8d019068)
