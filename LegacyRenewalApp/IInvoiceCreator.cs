using System;

namespace LegacyRenewalApp;

public interface IInvoiceCreator
{
    RenewalInvoice CreateInvoice(
        int customerId,
        Customer customer,
        string planCode,
        string paymentMethod,
        int seatCount,
        decimal baseAmount,
        decimal discountAmount,
        decimal supportFee,
        decimal paymentFee,
        decimal taxAmount,
        decimal finalAmount,
        string notes);
}
public class InvoiceCreator : IInvoiceCreator
{
    private readonly IInvoiceFactory _invoiceFactory;

    public InvoiceCreator(IInvoiceFactory invoiceFactory)
    {
        _invoiceFactory = invoiceFactory;
    }

    private string Normalize(string val) => val.Trim().ToUpperInvariant();

    private decimal Round(decimal val)
    {
        return Math.Round(val, 2,MidpointRounding.AwayFromZero);
    }

    public RenewalInvoice CreateInvoice(
        int customerId,
        Customer customer,
        string planCode,
        string paymentMethod,
        int seatCount,
        decimal baseAmount,
        decimal discountAmount,
        decimal supportFee,
        decimal paymentFee,
        decimal taxAmount,
        decimal finalAmount,
        string notes)
    {
        return _invoiceFactory.Create(
            customerId,
            customer,
            Normalize(planCode),
            Normalize(paymentMethod),
            seatCount,
            Round(baseAmount),
            Round(discountAmount),
            Round(supportFee),
            Round(paymentFee),
            Round(taxAmount),
            Round(finalAmount),
            notes);
    }
}
