using System;

namespace LegacyRenewalApp;

public interface ILoyaltyPointsService
{
    decimal CalculateDiscount(Customer customer, bool usePoints);
    string GetNotes(int pointsUsed);
}
public class LoyaltyPointsService : ILoyaltyPointsService
{
    public decimal CalculateDiscount(Customer customer, bool usePoints)
    {
        if (!usePoints || customer.LoyaltyPoints <= 0)
            return 0m;

        return Math.Min(customer.LoyaltyPoints, 200);
    }

    public string GetNotes(int pointsUsed)
        => $"loyalty points used: {pointsUsed}; ";
}

public interface IMinimumSubtotalPolicy
{
    decimal Apply(decimal subtotal);
    string GetNote(decimal original, decimal final);
}
public class MinimumSubtotalPolicy : IMinimumSubtotalPolicy
{
    private const decimal Min = 300m;

    public decimal Apply(decimal subtotal)
        => Math.Max(subtotal, Min);

    public string GetNote(decimal original, decimal final)
        => original < Min ? "minimum discounted subtotal applied; " : string.Empty;
}