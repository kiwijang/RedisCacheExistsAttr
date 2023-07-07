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

    public CountryController(ILogger<CountryController> logger, IMyService myService)
    {
        _logger = logger;
        _myService = myService;
    }

    [HttpGet(Name = "Country")]
    [RedisCacheExists(CacheKey = "GetCountryAsync")]
    public async Task<IEnumerable<string>> Get()
    {
        return await this._myService.GetCountryAsync("GetCountryAsync");
    }
}
