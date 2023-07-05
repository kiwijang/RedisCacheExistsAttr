using System;
using System.Collections.Generic;

namespace dooo.Models;

public partial class Country
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Continent { get; set; } = null!;

    public string Region { get; set; } = null!;

    public decimal SurfaceArea { get; set; }

    public short? IndepYear { get; set; }

    public int Population { get; set; }

    public decimal? LifeExpectancy { get; set; }

    public decimal? Gnp { get; set; }

    public decimal? Gnpold { get; set; }

    public string LocalName { get; set; } = null!;

    public string GovernmentForm { get; set; } = null!;

    public string? HeadOfState { get; set; }

    public int? Capital { get; set; }

    public string Code2 { get; set; } = null!;

    public virtual ICollection<City> Cities { get; set; } = new List<City>();

    public virtual ICollection<Countrylanguage> Countrylanguages { get; set; } = new List<Countrylanguage>();
}
