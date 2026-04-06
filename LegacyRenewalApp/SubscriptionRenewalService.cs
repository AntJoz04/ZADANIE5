using System;
using System.Collections.Generic;
using System.Linq;


namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        
        private readonly IBillingGateway _billingGateway;
        
        private readonly IMinimumSubtotalPolicy _minimumSubtotalPolicy;
       
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IMinimumInvoicePolicy _minimumInvoicePolicy;
        private readonly IInvoiceCreator _invoiceCreator;
        private readonly IInvoiceNotificationService _invoiceNotificationService;







        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            
            IBillingGateway billingGateway,
            
            IMinimumSubtotalPolicy minimumSubtotalPolicy,
            
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator,
            IMinimumInvoicePolicy minimumInvoicePolicy,
            IInvoiceCreator invoiceCreator,
            IInvoiceNotificationService invoiceNotificationService



    


        )
        {
            _customerRepository = customerRepository;
            _subscriptionPlanRepository = planRepository;
          
            _billingGateway = billingGateway;
            
            _minimumSubtotalPolicy = minimumSubtotalPolicy;
            
            _discountCalculator = discountCalculator;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxCalculator = taxCalculator;
            _minimumInvoicePolicy = minimumInvoicePolicy;
            _invoiceCreator = invoiceCreator;
            _invoiceNotificationService = invoiceNotificationService;





        }


        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository()
               ,
                new LegacyBillingGatewayAdapter(),
                
                new MinimumSubtotalPolicy(),
                new DiscountCalculator(                 
                    new List<IDiscountRule>
                    {
                        new SegmentDiscountRule(),
                        new LoyaltyDiscountRule(),
                        new SeatCountDiscountRule(),
                    },
                    new LoyaltyPointsService()),
                new SupportFeeCalculator(
                    new List<ISupportFeeStrategy>
                    {
                        new StartSupportFeeStrategy(),
                        new ProSupportFeeStrategy(),
                        new EnterpriseSupportFeeStrategy()
                    }),
                new PaymentFeeCalculator(
                    new List<IPaymentFeeStrategy>
                    {
                        new CardPaymentFeeStrategy(),
                        new BankTransferFeeStrategy(),
                        new PaypalFeeStrategy(),
                        new InvoiceFeeStrategy()
                    }),
                new TaxCalculator(
                    new List<ITaxStrategy>
                    {
                        new PolandTaxStrategy(),
                        new GermanyTaxStrategy(),
                        new CzechTaxStrategy(),
                        new NorwayTaxStrategy(),
                        new DefaultTaxStrategy()
                    }),
                new MinimumInvoicePolicy(),
                new InvoiceCreator(new InvoiceFactory()),
                new InvoiceNotificationService(new LegacyBillingGatewayAdapter())






            )
        {
        }

        private void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            
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

        private decimal ApplyMinimumSubtotalPolicy(decimal subtotal, ref string notes)
        {
            var adjusted = _minimumSubtotalPolicy.Apply(subtotal);
            notes += _minimumSubtotalPolicy.GetNote(subtotal, adjusted);
            return adjusted;
        }
        
        
        

        private void SaveInvoice(RenewalInvoice invoice)
        {
            _billingGateway.SaveInvoice(invoice);
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

            var (discountAmount, notes) = _discountCalculator.CalculateDiscounts(
                customer, plan, seatCount, baseAmount, useLoyaltyPoints);

            decimal subtotal = ApplyMinimumSubtotalPolicy(baseAmount - discountAmount, ref notes);

            decimal supportFee = _supportFeeCalculator.CalculateSupportFee(planCode, includePremiumSupport, ref notes);


            decimal paymentFee = _paymentFeeCalculator.CalculatePaymentFee(paymentMethod, subtotal + supportFee, ref notes);


            decimal taxAmount = _taxCalculator.CalculateTax(customer.Country, subtotal + supportFee + paymentFee);


            decimal finalAmount = _minimumInvoicePolicy.Apply(subtotal + supportFee + paymentFee + taxAmount, ref notes);


            var invoice = _invoiceCreator.CreateInvoice(
                customerId,
                customer,
                planCode,
                paymentMethod,
                seatCount,
                baseAmount,
                discountAmount,
                supportFee,
                paymentFee,
                taxAmount,
                finalAmount,
                notes);


            SaveInvoice(invoice);
            _invoiceNotificationService.SendInvoiceEmail(customer, planCode, invoice);


            return invoice;
        }

    }
}
