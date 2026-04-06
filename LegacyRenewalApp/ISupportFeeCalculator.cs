using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp;

public interface ISupportFeeCalculator
{
    decimal CalculateSupportFee(string planCode, bool includeSupport, ref string notes);
}
public class SupportFeeCalculator : ISupportFeeCalculator
{
    private readonly IEnumerable<ISupportFeeStrategy> _supportFeeStrategies;

    public SupportFeeCalculator(IEnumerable<ISupportFeeStrategy> supportFeeStrategies)
    {
        _supportFeeStrategies = supportFeeStrategies;
    }

    private string Normalize(string val) => val.Trim().ToUpperInvariant();

    public decimal CalculateSupportFee(string planCode, bool includeSupport, ref string notes)
    {
        if (!includeSupport)
            return 0m;

        var strategy = _supportFeeStrategies.First(s => s.CanHandle(Normalize(planCode)));
        notes += "premium support included; ";
        return strategy.CalculateFee();
    }
}
