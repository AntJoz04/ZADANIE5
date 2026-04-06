namespace LegacyRenewalApp;

public interface ITaxStrategy
{
    bool CanHandle(string country);
    decimal GetTaxRate();
}
public class PolandTaxStrategy : ITaxStrategy
{
    public bool CanHandle(string country)
        => country == "Poland";

    public decimal GetTaxRate()
        => 0.23m;
}
public class GermanyTaxStrategy : ITaxStrategy
{
    public bool CanHandle(string country)
        => country == "Germany";

    public decimal GetTaxRate()
        => 0.19m;
}
public class CzechTaxStrategy : ITaxStrategy
{
    public bool CanHandle(string country)
        => country == "Czech Republic";

    public decimal GetTaxRate()
        => 0.21m;
}
public class NorwayTaxStrategy : ITaxStrategy
{
    public bool CanHandle(string country)
        => country == "Norway";

    public decimal GetTaxRate()
        => 0.25m;
}
public class DefaultTaxStrategy : ITaxStrategy
{
    public bool CanHandle(string country)
        => true;

    public decimal GetTaxRate()
        => 0.20m;
}