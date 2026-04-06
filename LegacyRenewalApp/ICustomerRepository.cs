namespace LegacyRenewalApp;

public interface ICustomerRepository
{
    Customer GetById(int customerId);
}

public interface ISubscriptionPlanRepository
{
    SubscriptionPlan GetByCode(string code);
}