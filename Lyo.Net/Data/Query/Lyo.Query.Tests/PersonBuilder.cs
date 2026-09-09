namespace Lyo.Query.Tests;

public class PersonBuilder
{
    private readonly Person _person = new() {
        Name = "",
        Age = 0,
        Tags = new(),
        Id = Guid.Empty,
        Ts = DateTime.MinValue,
        D = default,
        DNullable = null,
        T = default,
        TNullable = null
    };

    public PersonBuilder WithName(string name)
    {
        _person.Name = name;
        return this;
    }

    public PersonBuilder WithAge(int age)
    {
        _person.Age = age;
        return this;
    }

    public PersonBuilder WithAgeNullable(int? age)
    {
        _person.AgeNullable = age;
        return this;
    }

    public PersonBuilder WithTags(params string[] tags)
    {
        _person.Tags = new(tags);
        return this;
    }

    public PersonBuilder WithId(Guid id)
    {
        _person.Id = id;
        return this;
    }

    public PersonBuilder WithIdNullable(Guid? id)
    {
        _person.IdNullable = id;
        return this;
    }

    public PersonBuilder WithTs(DateTime ts)
    {
        _person.Ts = ts;
        return this;
    }

    public PersonBuilder WithD(DateOnly d)
    {
        _person.D = d;
        return this;
    }

    public PersonBuilder WithDNullable(DateOnly? d)
    {
        _person.DNullable = d;
        return this;
    }

    public PersonBuilder WithT(TimeOnly t)
    {
        _person.T = t;
        return this;
    }

    public PersonBuilder WithTNullable(TimeOnly? t)
    {
        _person.TNullable = t;
        return this;
    }

    public PersonBuilder WithIsActive(bool active)
    {
        _person.IsActive = active;
        return this;
    }

    public PersonBuilder WithIsActiveNullable(bool? active)
    {
        _person.IsActiveNullable = active;
        return this;
    }

    public Person Build() => _person;
}