using System;
using System.Collections.Generic;
using System.Linq;


namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
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
        //pola do wstrzykiwania później zależnośći
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IEnumerable<IDiscountRule> _discountRules;
        private readonly IBillingGateway _billingGateway;
        private readonly IEnumerable<IPaymentFeeStrategy> _paymentStrategies;

        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IEnumerable<IDiscountRule> discountRules,
            IBillingGateway billingGateway,
            IEnumerable<IPaymentFeeStrategy> paymentStrategies)
        {
            //konsturktor z wsztrzykniętymi zależnościami
            _customerRepository = customerRepository;
            _subscriptionPlanRepository = planRepository;
            _discountRules = discountRules;
            _billingGateway=billingGateway;
            _paymentStrategies = paymentStrategies;
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
                    new LoyaltyPointsDiscountRule()
                },
                new LegacyBillingGatewayAdapter(),
                new List<IPaymentFeeStrategy>
                {
                    new CardPaymentFeeStrategy(),
                    new BankTransferFeeStrategy(),
                    new PaypalFeeStrategy(),
                    new InvoiceFeeStrategy()
                })
        {
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

            string normalizedPlanCode = Normalize(planCode);
            string normalizedPaymentMethod = Normalize(paymentMethod);

            var customer = _customerRepository.GetById(customerId);
            var plan = _subscriptionPlanRepository.GetByCode(normalizedPlanCode);
            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
          
            decimal discountAmount = 0m;
            string notes = string.Empty;

            foreach (var rule in _discountRules)
            {
                var result = rule.Apply(customer, plan, seatCount, baseAmount);
                discountAmount += result.Amount;
                notes += result.Notes;
            }
            

            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notes += $"loyalty points used: {pointsToUse}; ";
            }

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            decimal supportFee = 0m;
            if (includePremiumSupport)
            {
                if (normalizedPlanCode == "START")
                {
                    supportFee = 250m;
                }
                else if (normalizedPlanCode == "PRO")
                {
                    supportFee = 400m;
                }
                else if (normalizedPlanCode == "ENTERPRISE")
                {
                    supportFee = 700m;
                }

                notes += "premium support included; ";
            }

            decimal paymentFee = 0m;
            var strategy = _paymentStrategies.FirstOrDefault(s => s.CanHandle(normalizedPaymentMethod));
            if (strategy == null)
                throw new Exception("Unsupported payment method");
            paymentFee = strategy.Calculate(subtotalAfterDiscount + supportFee);
            notes += strategy.GetNote() + "; ";

            decimal taxRate = 0.20m;
            if (customer.Country == "Poland")
            {
                taxRate = 0.23m;
            }
            else if (customer.Country == "Germany")
            {
                taxRate = 0.19m;
            }
            else if (customer.Country == "Czech Republic")
            {
                taxRate = 0.21m;
            }
            else if (customer.Country == "Norway")
            {
                taxRate = 0.25m;
            }

            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}
