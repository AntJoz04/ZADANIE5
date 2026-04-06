namespace LegacyRenewalApp;

public interface ISupportFeeStrategy
{
    bool CanHandle(string planCode);
    decimal CalculateFee();
}
public class StartSupportFeeStrategy : ISupportFeeStrategy
{
    public bool CanHandle(string planCode) => planCode == "START";
    public decimal CalculateFee() => 250m;
}
public class ProSupportFeeStrategy : ISupportFeeStrategy
{
    public bool CanHandle(string planCode) => planCode == "PRO";
    public decimal CalculateFee() => 400m;
}
public class EnterpriseSupportFeeStrategy : ISupportFeeStrategy
{
    public bool CanHandle(string planCode) => planCode == "ENTERPRISE";
    public decimal CalculateFee() => 700m;
}