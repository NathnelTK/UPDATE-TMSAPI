using System.ComponentModel.DataAnnotations;

// --- Session 2 - Exercise 3: Options Pattern with Startup Validation ---
// PaymentOptions stores configuration settings for payment processing.
// We use DataAnnotations [Required] and [Range] to declare validation constraints.
// These constraints are validated at startup (ValidateOnStart()) rather than throwing runtime 500s later.
public class PaymentOptions
{
    [Required(ErrorMessage = "The GatewayUrl field is required.")]
    public required string GatewayUrl { get; init; }

    [Range(100.0, 100000.0, ErrorMessage = "MaxDepositBirr must be between 100 and 100,000.")]
    public decimal MaxDepositBirr { get; init; }
}
