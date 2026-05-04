using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

public sealed class StackTraceDecoderClassificationTests
{
    private const string MoqAndTestStack = """
Moq.MockException: Expected invocation on the mock at least once, but was never performed:
   at Moq.Mock.Verify(Mock mock, LambdaExpression expression, Times times, String failMessage)
   at MyApp.Tests.NotificationServiceTests.SendWelcomeEmail_ShouldCallEmailProvider() in NotificationServiceTests.cs:line 67
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
""";

    [Fact]
    public void Decode_Moq_Is_ThirdParty_When_Strict_UserPrefixes_Lists_MyApp_Only()
    {
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions {
            UserCodePrefixes = ["MyApp"],
            RestrictUserCodeToListedPrefixes = true
        });
        var trace = decoder.Decode(MoqAndTestStack);
        var moqFrame = trace.AllFrames.Single(f => f.FullMethod.Contains("Moq.Mock.Verify", StringComparison.Ordinal));
        Assert.Equal(FrameCategory.SystemOrThirdParty, moqFrame.Category);
        Assert.DoesNotContain("Moq", trace.UserNamespaces, StringComparer.Ordinal);
    }

    [Fact]
    public void Decode_Moq_Is_ThirdParty_Via_ExtraSystemPrefixes_LegacyMode()
    {
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions {
            ExtraSystemPrefixes = ["Moq."],
            RestrictUserCodeToListedPrefixes = false
        });
        var trace = decoder.Decode(MoqAndTestStack);
        var moqFrame = trace.AllFrames.Single(f => f.FullMethod.Contains("Moq.Mock.Verify", StringComparison.Ordinal));
        Assert.Equal(FrameCategory.SystemOrThirdParty, moqFrame.Category);
    }

    [Fact]
    public void Decode_StrictUserPrefixes_TreatsUnknownLibs_As_ThirdParty()
    {
        const string traceSample = """
System.Exception: x
   at ArbitraryVendorLib.Inner.DoWork() in V.cs:line 1
   at MyApp.Service.Run() in S.cs:line 2
""";
        var strict = new StackTraceDecoder(new StackTraceDecoderOptions {
            UserCodePrefixes = ["MyApp."],
            RestrictUserCodeToListedPrefixes = true
        });
        var decoded = strict.Decode(traceSample);
        Assert.Equal(FrameCategory.SystemOrThirdParty, decoded.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, decoded.AllFrames[1].Category);
    }

    [Fact]
    public void Decode_Legacy_UnclassifiedNamespace_Is_UserCode_When_Not_Strict()
    {
        const string traceSample = """
System.Exception: x
   at ArbitraryVendorLib.Inner.DoWork() in V.cs:line 1
""";
        var legacy = new StackTraceDecoder(new StackTraceDecoderOptions { RestrictUserCodeToListedPrefixes = false });
        var decoded = legacy.Decode(traceSample);
        Assert.Equal(FrameCategory.UserCode, decoded.AllFrames[0].Category);
    }

    [Fact]
    public void Decode_ExtraSystemPrefixes_Overrides_UnknownNamespace_UnderStrict()
    {
        const string traceSample = """
System.Exception: x
   at WeirdCo.Thing.Go() in W.cs:line 1
   at MyApp.Program.Main() in P.cs:line 1
""";
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions {
            UserCodePrefixes = ["MyApp."],
            ExtraSystemPrefixes = ["WeirdCo."],
            RestrictUserCodeToListedPrefixes = true
        });
        var decoded = decoder.Decode(traceSample);
        Assert.Equal(FrameCategory.SystemOrThirdParty, decoded.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, decoded.AllFrames[1].Category);
    }
}
