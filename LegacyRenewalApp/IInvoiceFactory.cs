using System;

namespace LegacyRenewalApp;

public interface IInvoiceFactory
{
    RenewalInvoice Create(
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

public class InvoiceFactory : IInvoiceFactory
{
    private decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
    public RenewalInvoice Create(
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
        return new RenewalInvoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{planCode}",
            CustomerName = customer.FullName,
            PlanCode = planCode,
            PaymentMethod = paymentMethod,
            SeatCount = seatCount,
            BaseAmount = Round(baseAmount),
            DiscountAmount = Round(discountAmount),
            SupportFee = Round(supportFee),
            PaymentFee = Round(paymentFee),
            TaxAmount = Round(taxAmount),
            FinalAmount = Round(finalAmount),
            Notes = notes.Trim(),
            GeneratedAt = DateTime.UtcNow
        };
    }
}

public interface IInvoicePersistenceService
{
    void Save(RenewalInvoice renewalInvoice);
}
public class InvoicePersistenceService : IInvoicePersistenceService
{
    private readonly IBillingGateway _billingGateway;

    public InvoicePersistenceService(IBillingGateway billingGateway)
    {
        _billingGateway = billingGateway;
    }

    public void Save(RenewalInvoice invoice)
    {
        _billingGateway.SaveInvoice(invoice);
    }
}