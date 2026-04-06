using System.Collections.Generic;

namespace LegacyRenewalApp;

public interface IDiscountCalculator
{
    (decimal discount, string notes) CalculateDiscounts(
        Customer customer,
        SubscriptionPlan plan,
        int seatCount,
        decimal baseAmount,
        bool useLoyaltyPoints);
}
public class DiscountCalculator : IDiscountCalculator
{
    private readonly IEnumerable<IDiscountRule> _discountRules;
    private readonly ILoyaltyPointsService _loyaltyPointsService;

    public DiscountCalculator(IEnumerable<IDiscountRule> discountRules,
        ILoyaltyPointsService loyaltyPointsService)
    {
        _discountRules = discountRules;
        _loyaltyPointsService = loyaltyPointsService;
    }

    public (decimal discount, string notes) CalculateDiscounts(
        Customer customer,
        SubscriptionPlan plan,
        int seatCount,
        decimal baseAmount,
        bool useLoyaltyPoints)
    {
        decimal discount = 0m;
        string notes = "";

        foreach (var rule in _discountRules)
        {
            var result = rule.Apply(customer, plan, seatCount, baseAmount);
            discount += result.Amount;
            notes += result.Notes;
        }

        var points = _loyaltyPointsService.CalculateDiscount(customer, useLoyaltyPoints);
        discount += points;

        if (points > 0)
            notes += _loyaltyPointsService.GetNotes((int)points);

        return (discount, notes);
    }
}

