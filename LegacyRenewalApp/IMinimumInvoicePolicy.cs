namespace LegacyRenewalApp;

public interface IMinimumInvoicePolicy
{
    decimal Apply(decimal amount, ref string notes);
}
public class MinimumInvoicePolicy : IMinimumInvoicePolicy
{
    public decimal Apply(decimal amount, ref string notes)
    {
        if (amount < 500m)
        {
            notes += "minimum invoice amount applied; ";
            return 500m;
        }

        return amount;
    }
}
