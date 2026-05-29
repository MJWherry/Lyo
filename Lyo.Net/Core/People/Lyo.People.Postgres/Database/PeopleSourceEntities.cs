using Lyo.EntityReference.Postgres.Database;

namespace Lyo.People.Postgres.Database;

public sealed class PersonSourceEntity : EntitySourceEntityBase
{
    public Guid PersonId { get; set; }

    public PersonEntity Person { get; set; } = null!;
}

public sealed class AddressSourceEntity : EntitySourceEntityBase
{
    public Guid AddressId { get; set; }

    public AddressEntity Address { get; set; } = null!;
}

public sealed class PhoneNumberSourceEntity : EntitySourceEntityBase
{
    public Guid PhoneNumberId { get; set; }

    public PhoneNumberEntity PhoneNumber { get; set; } = null!;
}

public sealed class EmailAddressSourceEntity : EntitySourceEntityBase
{
    public Guid EmailAddressId { get; set; }

    public EmailAddressEntity EmailAddress { get; set; } = null!;
}

public sealed class ContactAddressSourceEntity : EntitySourceEntityBase
{
    public Guid ContactAddressId { get; set; }

    public ContactAddressEntity ContactAddress { get; set; } = null!;
}

public sealed class ContactPhoneNumberSourceEntity : EntitySourceEntityBase
{
    public Guid ContactPhoneNumberId { get; set; }

    public ContactPhoneNumberEntity ContactPhoneNumber { get; set; } = null!;
}

public sealed class ContactEmailAddressSourceEntity : EntitySourceEntityBase
{
    public Guid ContactEmailAddressId { get; set; }

    public ContactEmailAddressEntity ContactEmailAddress { get; set; } = null!;
}