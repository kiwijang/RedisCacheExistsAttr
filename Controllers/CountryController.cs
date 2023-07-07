using dooo.Models;
using dooo.Services;
using Microsoft.AspNetCore.Mvc;
using MyAttributes;

namespace dooo.Controllers;

[ApiController]
[Route("[controller]")]
public class CountryController : ControllerBase
{
    private readonly ILogger<CountryController> _logger;
    private readonly IMyService _myService;

    private readonly string _cacheKey = "GetCountryAsync";

    public CountryController(ILogger<CountryController> logger, IMyService myService)
    {
        _logger = logger;
        _myService = myService;
    }

    [HttpGet(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<ActionResult<IEnumerable<string>>> Get()
    {
        // 快取存在
        if ((bool)(HttpContext.Items["IsRedisKeyExists"] ?? false))
        {
            return Ok(await this._myService.GetCacheDataByKey(_cacheKey));
        }
        // 快取不存在
        return Ok(await this._myService.GetCountryAsync(_cacheKey));
    }

    [HttpDelete(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<IActionResult> Delete(string code)
    {
        await this._myService.DeleteCountryAsync(_cacheKey, code);
        // 快取存在
        if ((bool)(HttpContext.Items["IsRedisKeyExists"] ?? false))
        {
            await this._myService.DeleteCacheDataByKey(_cacheKey);
        }
        return Ok();
    }

    [HttpPut(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<IActionResult> Put(Country entity)
    {
        await this._myService.UpdateCountryAsync(_cacheKey, entity);
        // 快取存在
        if ((bool)(HttpContext.Items["IsRedisKeyExists"] ?? false))
        {
            await this._myService.DeleteCacheDataByKey(_cacheKey);
        }
        return Ok();
    }

    [HttpPost(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<IActionResult> Create(Country entity)
    {
        await this._myService.CreateCountryAsync(_cacheKey, entity);
        // 快取存在
        if ((bool)(HttpContext.Items["IsRedisKeyExists"] ?? false))
        {
            await this._myService.DeleteCacheDataByKey(_cacheKey);
        }
        return Ok();
    }
}
