using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp;

public interface ITaxCalculator
{
    decimal CalculateTax(string country, decimal taxBase);
}

public class TaxCalculator : ITaxCalculator
{
    private readonly IEnumerable<ITaxStrategy> _taxStrategies;

    public TaxCalculator(IEnumerable<ITaxStrategy> taxStrategies)
    {
        _taxStrategies = taxStrategies;
    }

    public decimal CalculateTax(string country, decimal taxBase)
    {
        var strategy = _taxStrategies.FirstOrDefault(s => s.CanHandle(country))
                       ?? new DefaultTaxStrategy();

        return taxBase * strategy.GetTaxRate();
    }
}
