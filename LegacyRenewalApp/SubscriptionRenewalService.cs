using System;
using System.Collections.Generic;
using System.Linq;


namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {






        //pola do wstrzykiwania później zależnośći
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IEnumerable<IDiscountRule> _discountRules;
        private readonly IBillingGateway _billingGateway;
        private readonly IEnumerable<IPaymentFeeStrategy> _paymentStrategies;
        private readonly IEnumerable<ITaxStrategy> _taxStrategies;
        private readonly IEnumerable<ISupportFeeStrategy> _supportFeeStrategies;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly IMinimumSubtotalPolicy _minimumSubtotalPolicy;
        private readonly IInvoiceFactory _invoiceFactory;


        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IEnumerable<IDiscountRule> discountRules,
            IBillingGateway billingGateway,
            IEnumerable<IPaymentFeeStrategy> paymentStrategies,
            IEnumerable<ITaxStrategy> taxStrategies,
            IEnumerable<ISupportFeeStrategy> supportFeeStrategies,
            ILoyaltyPointsService loyaltyPointsService,
            IMinimumSubtotalPolicy minimumSubtotalPolicy,
            IInvoiceFactory invoiceFactory
        )
        {
            //konsturktor z wsztrzykniętymi zależnościami
            _customerRepository = customerRepository;
            _subscriptionPlanRepository = planRepository;
            _discountRules = discountRules;
            _billingGateway = billingGateway;
            _paymentStrategies = paymentStrategies;
            _taxStrategies = taxStrategies;
            _supportFeeStrategies = supportFeeStrategies;
            _loyaltyPointsService = loyaltyPointsService;
            _minimumSubtotalPolicy = minimumSubtotalPolicy;
            _invoiceFactory = invoiceFactory;
        }

        public SubscriptionRenewalService()
            //taki konstruktor domyślny by nie zmieniać Program.cs
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new List<IDiscountRule>
                {
                    new SegmentDiscountRule(),
                    new LoyaltyDiscountRule(),
                    new SeatCountDiscountRule(),

                },
                new LegacyBillingGatewayAdapter(),
                new List<IPaymentFeeStrategy>
                {
                    new CardPaymentFeeStrategy(),
                    new BankTransferFeeStrategy(),
                    new PaypalFeeStrategy(),
                    new InvoiceFeeStrategy()
                },
                new List<ITaxStrategy>
                {
                    new PolandTaxStrategy(),
                    new GermanyTaxStrategy(),
                    new CzechTaxStrategy(),
                    new NorwayTaxStrategy(),
                    new DefaultTaxStrategy()
                },
                new List<ISupportFeeStrategy>
                {
                    new StartSupportFeeStrategy(),
                    new ProSupportFeeStrategy(),
                    new EnterpriseSupportFeeStrategy()
                },
                new LoyaltyPointsService(),
                new MinimumSubtotalPolicy(),
                new InvoiceFactory())
        {
        }

        private void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            //metoda pomocnicza 1
            if (customerId <= 0)
                throw new ArgumentException("Customer id must be positive");

            if (string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("Plan code is required");

            if (seatCount <= 0)
                throw new ArgumentException("Seat count must be positive");

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required");
        }

        private string Normalize(string val)
        {
            //metoda pomocnicza 2
            return val.Trim().ToUpperInvariant();
        }

        private Customer LoadCustomer(int customerId)
        {
            var customer = _customerRepository.GetById(customerId);
            if (!customer.IsActive)
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            return customer;
        }

        private SubscriptionPlan LoadPlan(string planCode)
        {
            return _subscriptionPlanRepository.GetByCode(Normalize(planCode));
        }

        private decimal CalculateBaseAmount(SubscriptionPlan plan, int seatCount)
        {
            return (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
        }

        private (decimal discount, string notes) CalculateDiscounts(
            Customer customer,
            SubscriptionPlan plan,
            int seatCount,
            decimal baseAmount,
            bool useLoyaltyPoints)
        {
            decimal discount = 0m;
            string notes = "";

            foreach (var rule in _discountRules)
            {
                var result = rule.Apply(customer, plan, seatCount, baseAmount);
                discount += result.Amount;
                notes += result.Notes;
            }

            var points = _loyaltyPointsService.CalculateDiscount(customer, useLoyaltyPoints);
            discount += points;

            if (points > 0)
                notes += _loyaltyPointsService.GetNotes((int)points);

            return (discount, notes);
        }

        private decimal ApplyMinimumSubtotalPolicy(decimal subtotal, ref string notes)
        {
            var adjusted = _minimumSubtotalPolicy.Apply(subtotal);
            notes += _minimumSubtotalPolicy.GetNote(subtotal, adjusted);
            return adjusted;
        }

        private decimal CalculateSupportFee(string planCode, bool includeSupport, ref string notes)
        {
            if (!includeSupport)
                return 0m;

            var strategy = _supportFeeStrategies.First(s => s.CanHandle(Normalize(planCode)));
            notes += "premium support included; ";
            return strategy.CalculateFee();
        }

        private decimal CalculatePaymentFee(string paymentMethod, decimal amount, ref string notes)
        {
            var strategy = _paymentStrategies.FirstOrDefault(s => s.CanHandle(Normalize(paymentMethod)))
                           ?? throw new Exception("Unsupported payment method");

            notes += strategy.GetNote() + "; ";
            return strategy.Calculate(amount);
        }

        private decimal CalculateTax(string country, decimal taxBase)
        {
            var strategy = _taxStrategies.FirstOrDefault(s => s.CanHandle(country))
                           ?? new DefaultTaxStrategy();

            return taxBase * strategy.GetTaxRate();
        }

        private decimal ApplyMinimumInvoiceAmount(decimal amount, ref string notes)
        {
            if (amount < 500m)
            {
                notes += "minimum invoice amount applied; ";
                return 500m;
            }

            return amount;
        }

        private RenewalInvoice CreateInvoice(
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
                baseAmount,
                discountAmount,
                supportFee,
                paymentFee,
                taxAmount,
                finalAmount,
                notes);
        }

        private void SaveInvoice(RenewalInvoice invoice)
        {
            _billingGateway.SaveInvoice(invoice);
        }

        private void SendEmailIfPossible(Customer customer, string planCode, RenewalInvoice invoice)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
                return;

            string subject = "Subscription renewal invoice";
            string body =
                $"Hello {customer.FullName}, your renewal for plan {Normalize(planCode)} " +
                $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

            _billingGateway.SendEmail(customer.Email, subject, body);
        }


        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInput(customerId, planCode, seatCount, paymentMethod);

            var customer = LoadCustomer(customerId);
            var plan = LoadPlan(planCode);

            decimal baseAmount = CalculateBaseAmount(plan, seatCount);

            var (discountAmount, notes) = CalculateDiscounts(customer, plan, seatCount, baseAmount, useLoyaltyPoints);

            decimal subtotal = ApplyMinimumSubtotalPolicy(baseAmount - discountAmount, ref notes);

            decimal supportFee = CalculateSupportFee(planCode, includePremiumSupport, ref notes);

            decimal paymentFee = CalculatePaymentFee(paymentMethod, subtotal + supportFee, ref notes);

            decimal taxAmount = CalculateTax(customer.Country, subtotal + supportFee + paymentFee);

            decimal finalAmount = ApplyMinimumInvoiceAmount(subtotal + supportFee + paymentFee + taxAmount, ref notes);

            var invoice = CreateInvoice(customerId, customer, planCode, paymentMethod, seatCount,
                baseAmount, discountAmount, supportFee, paymentFee, taxAmount, finalAmount, notes);

            SaveInvoice(invoice);
            SendEmailIfPossible(customer, planCode, invoice);

            return invoice;
        }

    }
}
