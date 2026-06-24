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
  City?: string | null;
  State?: string | null;
  County?: string | null;
  Zipcode?: string | null;
  Zipcode4?: string | null;
  CreatedDate: string;
  UpdatedDate: string;
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
  Type?: string | null;
  CreatedDate: string;
  UpdatedDate: string;
}

export interface PersonRes {
  Id: string;
  EndatoPersonId?: string | null;
  Prefix?: string | null;
  FirstName: string;
  MiddleName?: string | null;
  LastName: string;
  Suffix?: string | null;
  Source: string;
  Addresses?: PersonAddressRes[] | null;
  EmailAddresses?: PersonEmailAddressRes[] | null;
  PhoneNumbers?: PersonPhoneNumberRes[] | null;
}
