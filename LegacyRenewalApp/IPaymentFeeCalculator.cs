using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp;

public interface IPaymentFeeCalculator
{
    decimal CalculatePaymentFee(string paymentMethod, decimal amount, ref string notes);
}
public class PaymentFeeCalculator : IPaymentFeeCalculator
{
    private readonly IEnumerable<IPaymentFeeStrategy> _paymentStrategies;

    public PaymentFeeCalculator(IEnumerable<IPaymentFeeStrategy> paymentStrategies)
    {
        _paymentStrategies = paymentStrategies;
    }

    private string Normalize(string val) => val.Trim().ToUpperInvariant();

    public decimal CalculatePaymentFee(string paymentMethod, decimal amount, ref string notes)
    {
        var strategy = _paymentStrategies.FirstOrDefault(s => s.CanHandle(Normalize(paymentMethod)))
                       ?? throw new Exception("Unsupported payment method");

        notes += strategy.GetNote() + "; ";
        return strategy.Calculate(amount);
    }
}
