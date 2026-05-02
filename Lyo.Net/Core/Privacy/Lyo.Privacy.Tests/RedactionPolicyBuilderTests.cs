using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Tests;

public sealed class RedactionPolicyBuilderTests
{
    [Fact]
    public void AddPhoneRule_mode_helper_matches_ctor()
    {
        var a = new RedactionPolicyBuilder().AddPhoneRule(PhoneMaskMode.FirstDigitOfLastGroup).Build();
        var b = new RedactionPolicyBuilder().AddRule(new PhoneRedactionRule(PhoneMaskMode.FirstDigitOfLastGroup)).Build();
        Assert.Equal(b.Rules.Count, a.Rules.Count);
        Assert.IsType<PhoneRedactionRule>(a.Rules[0]);
    }

    [Fact]
    public void AddRule_generic_delegates_to_AddRule()
    {
        var p = new RedactionPolicyBuilder().AddRule(new LiteralSubstringRedactionRule("x")).Build();
        var q = new RedactionPolicyBuilder().AddRule(new LiteralSubstringRedactionRule("x")).Build();
        Assert.Equal(p.Rules.Count, q.Rules.Count);
        Assert.IsType<LiteralSubstringRedactionRule>(q.Rules[0]);
    }
}