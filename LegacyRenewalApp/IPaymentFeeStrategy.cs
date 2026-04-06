namespace LegacyRenewalApp;

public interface IPaymentFeeStrategy
{
    bool CanHandle(string paymentMethod);
    decimal Calculate(decimal amount);
    string GetNote();
}
public class CardPaymentFeeStrategy : IPaymentFeeStrategy
{
    public bool CanHandle(string paymentMethod)
        => paymentMethod == "CARD";

    public decimal Calculate(decimal amount)
        => amount * 0.02m;

    public string GetNote()
        => "card payment fee";
}
public class BankTransferFeeStrategy : IPaymentFeeStrategy
{
    public bool CanHandle(string paymentMethod)
        => paymentMethod == "BANK_TRANSFER";

    public decimal Calculate(decimal amount)
        => amount * 0.01m;

    public string GetNote()
        => "bank transfer fee";
}
public class PaypalFeeStrategy : IPaymentFeeStrategy
{
    public bool CanHandle(string paymentMethod)
        => paymentMethod == "PAYPAL";

    public decimal Calculate(decimal amount)
        => amount * 0.035m;

    public string GetNote()
        => "paypal fee";
}
public class InvoiceFeeStrategy : IPaymentFeeStrategy
{
    public bool CanHandle(string paymentMethod)
        => paymentMethod == "INVOICE";

    public decimal Calculate(decimal amount)
        => 0m;

    public string GetNote()
        => "invoice payment";
}