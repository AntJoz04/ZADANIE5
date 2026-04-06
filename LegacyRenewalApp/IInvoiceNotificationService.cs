namespace LegacyRenewalApp;

public interface IInvoiceNotificationService
{
    void SendInvoiceEmail(Customer customer, string planCode, RenewalInvoice invoice);
}
public class InvoiceNotificationService : IInvoiceNotificationService
{
    private readonly IBillingGateway _billingGateway;

    public InvoiceNotificationService(IBillingGateway billingGateway)
    {
        _billingGateway = billingGateway;
    }

    private string Normalize(string val) => val.Trim().ToUpperInvariant();

    public void SendInvoiceEmail(Customer customer, string planCode, RenewalInvoice invoice)
    {
        if (string.IsNullOrWhiteSpace(customer.Email))
            return;

        string subject = "Subscription renewal invoice";
        string body =
            $"Hello {customer.FullName}, your renewal for plan {Normalize(planCode)} " +
            $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

        _billingGateway.SendEmail(customer.Email, subject, body);
    }
}
