using System;

namespace LegacyRenewalApp;

public class DiscountResult
{
    public decimal Amount { get; set; }
    public string Notes {get; set;}=string.Empty;
}
public interface IDiscountRule
{
    DiscountResult Apply(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount);
}
public class SegmentDiscountRule : IDiscountRule
{
    public DiscountResult Apply(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount)
    {
        var result = new DiscountResult();

        switch (customer.Segment)
        {
            case "Silver":
                result.Amount = baseAmount * 0.05m;
                result.Notes = "silver discount; ";
                break;

            case "Gold":
                result.Amount = baseAmount * 0.10m;
                result.Notes = "gold discount; ";
                break;

            case "Platinum":
                result.Amount = baseAmount * 0.15m;
                result.Notes = "platinum discount; ";
                break;

            case "Education" when plan.IsEducationEligible:
                result.Amount = baseAmount * 0.20m;
                result.Notes = "education discount; ";
                break;
        }

        return result;
    }
}
public class LoyaltyDiscountRule : IDiscountRule
{
    public DiscountResult Apply(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount)
    {
        var result = new DiscountResult();

        if (customer.YearsWithCompany >= 5)
        {
            result.Amount = baseAmount * 0.07m;
            result.Notes = "long-term loyalty discount; ";
        }
        else if (customer.YearsWithCompany >= 2)
        {
            result.Amount = baseAmount * 0.03m;
            result.Notes = "basic loyalty discount; ";
        }

        return result;
    }
}
public class SeatCountDiscountRule : IDiscountRule
{
    public DiscountResult Apply(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount)
    {
        var result = new DiscountResult();

        if (seatCount >= 50)
        {
            result.Amount = baseAmount * 0.12m;
            result.Notes = "large team discount; ";
        }
        else if (seatCount >= 20)
        {
            result.Amount = baseAmount * 0.08m;
            result.Notes = "medium team discount; ";
        }
        else if (seatCount >= 10)
        {
            result.Amount = baseAmount * 0.04m;
            result.Notes = "small team discount; ";
        }

        return result;
    }
}

