using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

public sealed class StackTraceDecoderRecursionTests
{
    private const string MutualOverflowSample = """
System.StackOverflowException: The requested operation caused a stack overflow.
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 77
   at MyApp.Parsers.ExpressionParser.ParseExpression(TokenStream tokens) in ExpressionParser.cs:line 61
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 80
   at MyApp.Parsers.ExpressionParser.ParseExpression(TokenStream tokens) in ExpressionParser.cs:line 61
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 80
   ... [repeated]
   at MyApp.Parsers.ExpressionParser.Parse(String input) in ExpressionParser.cs:line 22
""";

    [Fact]
    public void Decode_ParserMutualRecursion_SixFrameCycle_WorksUnderStrictUserCode()
    {
        const string stack = """
System.StackOverflowException: The requested operation caused a stack overflow.
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 77
   at MyApp.Parsers.ExpressionParser.ParseExpression(TokenStream tokens) in ExpressionParser.cs:line 61
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 80
   at MyApp.Parsers.ExpressionParser.ParseExpression(TokenStream tokens) in ExpressionParser.cs:line 61
   at MyApp.Parsers.ExpressionParser.ParseTerm(TokenStream tokens) in ExpressionParser.cs:line 80
   at MyApp.Parsers.ExpressionParser.ParseExpression(TokenStream tokens) in ExpressionParser.cs:line 61
""";
        var strict = new StackTraceDecoder(new StackTraceDecoderOptions { RestrictUserCodeToListedPrefixes = true });
        var trace = strict.Decode(stack);
        Assert.True(trace.HasRecursion);
        Assert.Equal(6, trace.RecursionPatterns[0].Depth);
    }

    [Fact]
    public void Decode_Detects_AlternatingMutualRecursion_BeforeTailFrame()
    {
        var decoder = new StackTraceDecoder();
        var trace = decoder.Decode(MutualOverflowSample);
        Assert.True(trace.HasRecursion);
        Assert.NotEmpty(trace.RecursionPatterns);
        var r = trace.RecursionPatterns[0];
        Assert.True(r.Depth >= 4, $"expected at least 4 frames in cycle run, got {r.Depth}");
        Assert.Contains("ParseTerm", r.Frame.FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_StillDetects_DirectRecursion_SinglePeriod()
    {
        const string direct = """
System.Exception: x
   at MyApp.Foo() in A.cs:line 1
   at MyApp.Foo() in A.cs:line 2
   at MyApp.Foo() in A.cs:line 3
""";
        var decoder = new StackTraceDecoder();
        var trace = decoder.Decode(direct);
        Assert.True(trace.HasRecursion);
        Assert.Equal(3, trace.RecursionPatterns[0].Depth);
    }

    [Fact]
    public void Decode_DoesNotFlag_MoqVerify_Test_MethodInvoker_FourDistinctFrames()
    {
        const string stack = """
Moq.MockException:
Expected invocation on the mock at least once, but was never performed:
   mock => mock.SendEmail(It.IsAny<String>(), It.IsAny<String>())

   at Moq.Mock.Verify(Mock mock, LambdaExpression expression, Times times, String failMessage)
   at MyApp.Tests.NotificationServiceTests.SendWelcomeEmail_ShouldCallEmailProvider() in NotificationServiceTests.cs:line 67
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
""";
        var trace = new StackTraceDecoder().Decode(stack);
        Assert.False(trace.HasRecursion);
    }

    [Fact]
    public void Decode_DoesNotFlag_ThreeDistinctFrames_When_ThresholdIsThree()
    {
        const string stack = """
System.Exception: x
   at MyApp.Alpha() in A.cs:line 1
   at MyApp.Beta() in B.cs:line 1
   at MyApp.Gamma() in C.cs:line 1
""";
        var trace = new StackTraceDecoder().Decode(stack);
        Assert.False(trace.HasRecursion);
    }

    [Fact]
    public void Decode_DoesNotFlag_ShortAccidentalRepeat()
    {
        const string shortAlt = """
System.Exception: x
   at MyApp.A() in A.cs:line 1
   at MyApp.B() in B.cs:line 1
   at MyApp.A() in A.cs:line 2
   at MyApp.B() in B.cs:line 2
""";
        var decoder = new StackTraceDecoder(new StackTraceDecoderOptions { RecursionThreshold = 5 });
        var trace = decoder.Decode(shortAlt);
        Assert.False(trace.HasRecursion);
    }
}
