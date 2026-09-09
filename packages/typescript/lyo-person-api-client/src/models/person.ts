export interface PersonPointRes {
    X: number;
    Y: number;
}

export interface PersonAddressRes {
    Id: string;
    PersonId: string;
    HouseNumber?: string | null;
    StreetPreDirection?: string | null;
    StreetName?: string | null;
    StreetPostDirection?: string | null;
    StreetType?: string | null;
    Unit?: string | null;
    UnitType?: string | null;
    StreetAddress?: string | null;
    StreetAddressLine2?: string | null;
    City?: string | null;
    State?: string | null;
    County?: string | null;
    Zipcode?: string | null;
    Zipcode4?: string | null;
    PostalCode?: string | null;
    CountryCode: string;
    FullAddress?: string | null;
    Coordinates?: PersonPointRes | null;
    SourceEntityType?: string | null;
    SourceEntityId?: string | null;
    ImportedAt?: string | null;
    CreatedTimestamp: string;
    UpdatedTimestamp?: string | null;
}

export interface PersonEmailAddressRes {
    Id: string;
    PersonId: string;
    Address: string;
}

export interface PersonPhoneNumberRes {
    Id: string;
    PersonId: string;
    Number: string;
    CountryCode?: string | null;
    CountryCodeString?: string | null;
    TechnologyType?: string | null;
    VerifiedAt?: string | null;
    Label?: string | null;
    Type?: string | null;
    SourceEntityType?: string | null;
    SourceEntityId?: string | null;
    ImportedAt?: string | null;
    CreatedTimestamp: string;
    UpdatedTimestamp?: string | null;
}

export interface PersonRes {
    Id: string;
    EndatoPersonId?: string | null;
    CreatedTimestamp: string;
    UpdatedTimestamp?: string | null;
    LocallyModifiedAt?: string | null;
    CreatedBy?: string | null;
    SourceEntityType?: string | null;
    SourceEntityId?: string | null;
    ImportedAt?: string | null;
    NamePrefix?: string | null;
    FirstName: string;
    MiddleName?: string | null;
    LastName: string;
    NameSuffix?: string | null;
    PreferredName?: string | null;
    MaidenName?: string | null;
    DateOfBirth?: string | null;
    Sex?: string | null;
    Nationality?: string | null;
    PreferredLanguageBcp47?: string | null;
    Race?: string | null;
    MaritalStatus?: string | null;
    DisabilityStatus?: string | null;
    VeteranStatus?: string | null;
    PlaceOfBirthAddressId?: string | null;
    EmergencyContactPersonId?: string | null;
    CurrentJobTitle?: string | null;
    CurrentCompany?: string | null;
    IsActive: boolean;
    Notes?: string | null;
    CitizenshipJson?: string | null;
    PreferencesJson?: string | null;
    CustomFieldsJson?: string | null;
    Addresses?: PersonAddressRes[] | null;
    EmailAddresses?: PersonEmailAddressRes[] | null;
    PhoneNumbers?: PersonPhoneNumberRes[] | null;
}
