namespace Infrastructure;

/// <summary>
/// Tunable knobs for the mock payment gateway. All values come from configuration;
/// none are defaulted here to avoid silently running with wrong settings.
/// </summary>
public sealed class PaymentGatewayOptions
{
    /// <summary>Probability (0–1) that a cash payment is declined.</summary>
    public double CashFailureRate { get; set; }

    /// <summary>Probability (0–1) that a card payment is declined.</summary>
    public double CardFailureRate { get; set; }

    /// <summary>Artificial delay added to every call to simulate network latency (ms).</summary>
    public int SimulatedLatencyMs { get; set; }
}
