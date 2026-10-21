namespace Lyo.Billing;

/// <summary>Stable <c>EntityRef.EntityType</c> strings for billing client links. Hosts that wire People/Geolocation persist the same names.</summary>
public static class BillingEntityTypes
{
    /// <summary>Bill-to party stored in this plugin.</summary>
    public const string BillingClient = "BillingClient";

    /// <summary>Lite person payload owned by billing (when People is not wired).</summary>
    public const string BillingPerson = "BillingPerson";

    /// <summary>Lite phone payload owned by billing.</summary>
    public const string BillingPhoneNumber = "BillingPhoneNumber";

    /// <summary>Lite email payload owned by billing.</summary>
    public const string BillingEmailAddress = "BillingEmailAddress";

    /// <summary>Lite address payload owned by billing.</summary>
    public const string BillingAddress = "BillingAddress";

    /// <summary>Core People person. Billing does not reference the People package; hosts pass this type string.</summary>
    public const string Person = "Person";

    /// <summary>People phone number payload.</summary>
    public const string PhoneNumber = "PhoneNumber";

    /// <summary>People email address payload.</summary>
    public const string EmailAddress = "EmailAddress";

    /// <summary>Geolocation address payload.</summary>
    public const string Address = "Address";
}