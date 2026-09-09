using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

/// <summary>Rich example stack traces and expectations for <see cref="StackTraceDecoder" /> behaviour (classification, crash site, recursion, parsing).</summary>
public sealed class StackTraceDecoderScenarioTests
{
    private static readonly StackTraceDecoder DefaultDecoder = new();
    private static readonly StackTraceDecoder StrictDecoder = new(new() { RestrictUserCodeToListedPrefixes = true });

#region Frame order and user frame list

    [Fact]
    public void Decode_UserFrames_AreInnermostFirstMatchingAllFramesOrder()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Inner.Deepest() in Inner.cs:line 1
                                at MyApp.Middle.Call() in Middle.cs:line 2
                                at MyApp.Outer.Entry() in Outer.cs:line 3
                                at System.Console.WriteLine()
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(4, d.TotalFrameCount);
        Assert.Equal(3, d.UserFrameCount);
        Assert.Equal("Deepest", d.UserFrames[0].MethodName);
        Assert.Equal("Call", d.UserFrames[1].MethodName);
        Assert.Equal("Entry", d.UserFrames[2].MethodName);
        Assert.Equal("Deepest", d.DeepestUserFrame!.MethodName);
    }

#endregion

#region Classification — BCL / Microsoft platform, ExtraSystemPrefixes, test framework

    [Fact]
    public void Decode_PureSystemTrace_HasNoUserFrames()
    {
        const string trace = """
                             System.ArgumentNullException: Value cannot be null.
                                at System.ThrowHelper.ThrowArgumentNullException(ExceptionArgument argument)
                                at System.String.Replace(String oldValue, String newValue)
                                at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine& stateMachine)
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(3, d.TotalFrameCount);
        Assert.Equal(0, d.UserFrameCount);
        Assert.Equal(3, d.SystemFrameCount);
        Assert.Null(d.LikelyCrashSite);
        Assert.False(d.HasRecursion);
    }

    [Fact]
    public void Decode_DefaultDecoder_TreatsUnconfiguredNuGetStyleNamespaceAsUserCode()
    {
        const string trace = """
                             FluentValidation.ValidationException: Validation failed
                                at FluentValidation.AbstractValidator.Validate(ValidationContext context)
                                at Newtonsoft.Json.JsonTextReader.Read()
                                at MyApp.Program.Entry() in Program.cs:line 1
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[2].Category);
        Assert.Equal(3, d.UserFrameCount);
    }

    [Fact]
    public void Decode_ExtraSystemPrefixes_ClassifiesListedVendorsAsThirdParty()
    {
        const string trace = """
                             System.Exception: x
                                at FluentValidation.AbstractValidator.Validate(ValidationContext context)
                                at Npgsql.Internal.NpgsqlConnector.ReadMessage()
                                at MyApp.Data.Orders.Load() in Orders.cs:line 88
                             """;

        var d = new StackTraceDecoder(new() { ExtraSystemPrefixes = ["FluentValidation.", "Npgsql."] }).Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[1].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[2].Category);
        Assert.Single(d.UserFrames);
    }

    [Fact]
    public void Decode_EF_CoreAndSqlClientClassifiedAsThirdParty()
    {
        const string trace = """
                             Microsoft.Data.SqlClient.SqlException: Timeout expired
                                at Microsoft.Data.SqlClient.SqlCommand.ExecuteReader()
                                at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReader(RelationalCommandParameterObject parameterObject)
                                at MyApp.Data.OrderRepository.GetById(Int32 id) in OrderRepository.cs:line 101
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[1].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[2].Category);
        Assert.Equal(3, d.TotalFrameCount);
    }

    [Fact]
    public void Decode_OperationCanceled_ClassifiedAsSystemInnermostUserStillSelectedWhenPresent()
    {
        const string trace = """
                             System.OperationCanceledException: The operation was canceled.
                                at System.Threading.CancellationToken.ThrowOperationCanceledException()
                                at MyApp.Jobs.Indexer.Run(CancellationToken ct) in Indexer.cs:line 50
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
        Assert.Contains("Indexer.Run", d.LikelyCrashSite!.FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_Xunit_FrameClassifiedAsTestFramework()
    {
        const string trace = """
                             Xunit.Sdk.FalseException: Assert.False() Failure
                                at Xunit.Assert.False(Boolean condition) in Assert.cs:line 52
                                at MyApp.Tests.UnitTests.Feature_Is_Covered() in UnitTests.cs:line 28
                             """;

        var d = DefaultDecoder.Decode(trace);
        var xunitFrame = d.AllFrames.Single(f => f.FullMethod.Contains("Xunit.Assert.False", StringComparison.Ordinal));
        var testFrame = d.AllFrames.Single(f => f.FullMethod.Contains("MyApp.Tests.UnitTests", StringComparison.Ordinal));
        Assert.Equal(FrameCategory.TestFramework, xunitFrame.Category);
        Assert.Equal(FrameCategory.UserCode, testFrame.Category);
        Assert.Single(d.TestFrames);
    }

    [Fact]
    public void Decode_NUnit_FrameClassifiedAsTestFramework()
    {
        const string trace = """
                             NUnit.Framework.AssertionException: Expected: not null
                                at NUnit.Framework.Assert.That(String actual, IResolveConstraint constraint)
                                at MyApp.Tests.Suite.Sample_Test() in Suite.cs:line 15
                             """;

        var d = DefaultDecoder.Decode(trace);
        var nunitFrame = d.AllFrames.First(f => f.FullMethod.Contains("NUnit.Framework.Assert.That", StringComparison.Ordinal));
        Assert.Equal(FrameCategory.TestFramework, nunitFrame.Category);
        Assert.Equal(FrameCategory.UserCode, d.UserFrames[0].Category);
    }

    [Fact]
    public void Decode_Microsoft_AspNetCoreMvcClassifiedAsThirdParty()
    {
        const string trace = """
                             System.InvalidOperationException: bad
                                at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()
                                at MyApp.Controllers.HealthController.Get() in HealthController.cs:line 9
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
    }

    [Fact]
    public void Decode_UserPrefix_OverridesBuiltInSystemList()
    {
        const string trace = """
                             System.Exception: x
                                at Microsoft.Internal.MyProduct.Core.Service.Run() in S.cs:line 1
                             """;

        var d = new StackTraceDecoder(new() { UserCodePrefixes = ["Microsoft.Internal.MyProduct."] }).Decode(trace);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[0].Category);
    }

#endregion

#region Strict user-code mode

    [Fact]
    public void Decode_StrictEmptyUserPrefixesEverythingNonTest_IsThirdPartyIncludingMyApp()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Lib.Helper.Go() in H.cs:line 1
                             """;

        var d = StrictDecoder.Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(0, d.UserFrameCount);
    }

    [Fact]
    public void Decode_StrictWithMyAppPrefixOnlyMyApp_IsUser()
    {
        const string trace = """
                             System.Exception: x
                                at Contoso.Sdk.Client.Call() in C.cs:line 1
                                at MyApp.Services.Orchestrator.Run() in O.cs:line 5
                             """;

        var d = new StackTraceDecoder(new() { UserCodePrefixes = ["MyApp."], RestrictUserCodeToListedPrefixes = true }).Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
        Assert.Equal("O.cs:5", d.UserFrames[0].LocationSummary);
    }

#endregion

#region Inner exceptions and crash / deepest

    [Fact]
    public void Decode_InnerException_WinsForLikelyCrashSiteAndDeepest()
    {
        const string trace = """
                             System.Net.Http.HttpRequestException: Request failed
                                at System.Net.Http.HttpClient.GetStringAsync()
                                at MyApp.Clients.ApiClient.Fetch(String url) in ApiClient.cs:line 30
                              ---> System.IO.FileNotFoundException: missing
                                at MyApp.Storage.FileCache.Read(String key) in FileCache.cs:line 12
                                --- End of inner exception stack trace ---
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Single(d.InnerExceptions);
        Assert.Contains("FileCache", d.LikelyCrashSite!.FullMethod, StringComparison.Ordinal);
        Assert.Contains("FileCache", d.DeepestUserFrame!.FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_Inner_HasNoUserFrameFallbackToOuterUser()
    {
        const string trace = """
                             System.Exception: outer failed
                                at MyApp.Service.Handle() in Service.cs:line 100
                              ---> System.OutOfMemoryException: oom
                                at System.Runtime.GC.AllocateNew()
                                at System.Buffer.MemoryCopy()
                                --- End of inner exception stack trace ---
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Contains("Service.Handle", d.LikelyCrashSite!.FullMethod, StringComparison.Ordinal);
        Assert.Contains("Service.Handle", d.DeepestUserFrame!.FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_NestedInnersDeepestInnerUser_Wins()
    {
        const string trace = """
                             System.Exception: One
                                at MyApp.Layer.A() in A.cs:line 1
                              ---> System.Exception: Two
                                at MyApp.Layer.B() in B.cs:line 2
                              ---> System.ArgumentException: Three
                                at MyApp.Layer.C() in C.cs:line 3
                                --- End of inner exception stack trace ---
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(2, d.InnerExceptions.Count);
        Assert.Contains("Layer.C", d.LikelyCrashSite!.FullMethod, StringComparison.Ordinal);
        Assert.Contains("Layer.C", d.DeepestUserFrame!.FullMethod, StringComparison.Ordinal);
    }

#endregion

#region Async and decoder options

    [Fact]
    public void Decode_StripAsyncNoise_RemovesCompilerGeneratedFrameFromUserAndGroups()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Worker+<FetchOrder>d__3.MoveNext()
                                at MyApp.Services.OrderService.Place() in OrderService.cs:line 20
                             """;

        var stripped = new StackTraceDecoder(new() { StripAsyncNoise = true }).Decode(trace);
        var full = DefaultDecoder.Decode(trace);
        Assert.Equal(2, stripped.TotalFrameCount);
        Assert.Equal(2, full.UserFrameCount);
        Assert.Equal(1, stripped.UserFrameCount);
        Assert.DoesNotContain("MoveNext", stripped.UserFrames[0].FullMethod, StringComparison.Ordinal);
        Assert.Contains("OrderService", stripped.UserFrames[0].FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_RecursionThresholdTwo_FlagsPairOfIdenticalUserFrames()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Idempotent.Retry() in R.cs:line 1
                                at MyApp.Idempotent.Retry() in R.cs:line 2
                             """;

        var d = new StackTraceDecoder(new() { RecursionThreshold = 2 }).Decode(trace);
        Assert.True(d.HasRecursion);
        Assert.Equal(2, d.RecursionPatterns[0].Depth);
    }

#endregion

#region More frameworks and edge cases

    [Fact]
    public void Decode_MSTests_AssertClassifiedAsTestFramework()
    {
        const string trace = """
                             Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException: Expected:<2>. Actual:<3>.
                                at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual[T](T expected, T actual)
                                at MyApp.Tests.MathTests.Add_Returns_Sum() in MathTests.cs:line 22
                             """;

        var d = DefaultDecoder.Decode(trace);
        var assertFrame = d.AllFrames.First(f => f.FullMethod.Contains("Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual", StringComparison.Ordinal));
        Assert.Equal(FrameCategory.TestFramework, assertFrame.Category);
        Assert.Equal(FrameCategory.UserCode, d.UserFrames[0].Category);
    }

    [Fact]
    public void Decode_RefitWithoutConfig_IsUserCode()
    {
        const string trace = """
                             Refit.ApiException: Response status code does not indicate success: 500
                                at Refit.Implementation.Generated.MyApi.<RemotingInvokeAsync>d__45.MoveNext()
                                at MyApp.Jobs.SyncJob.Run() in SyncJob.cs:line 8
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
    }

    [Fact]
    public void Decode_UserNamespaces_AreDistinctAndSorted()
    {
        const string trace = """
                             System.Exception: x
                                at LibZed.Handler.Use() in Z.cs:line 1
                                at LibAlfa.Handler.Entry() in A.cs:line 2
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(["LibAlfa", "LibZed"], d.UserNamespaces);
    }

    [Fact]
    public void Decode_Inner_ExceptionWithNoInnerStackFramesFallbackCrashToOuterUser()
    {
        const string trace = """
                             System.IO.IOException: Write failed
                                at MyApp.Export.Writer.Flush() in Writer.cs:line 90
                              ---> System.ComponentModel.Win32Exception: The device is not ready.
                                --- End of inner exception stack trace ---
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Single(d.InnerExceptions);
        Assert.Equal(0, d.InnerExceptions[0].TotalFrameCount);
        Assert.Contains("Writer.Flush", d.LikelyCrashSite!.FullMethod, StringComparison.Ordinal);
        Assert.Contains("Writer.Flush", d.DeepestUserFrame!.FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_Generic_TypeArityInMethodStringStillUserCode()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Caching.Cache`1.Get(String key) in Cache.cs:line 15
                                at MyApp.Api.UsersController.Get(Int32 id) in UsersController.cs:line 40
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(2, d.UserFrameCount);
        Assert.Contains("Cache`1.Get", d.UserFrames[0].FullMethod, StringComparison.Ordinal);
        Assert.Contains("UsersController.Get", d.UserFrames[1].FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_SourceFileWithSpacesInPath_ParsesLine()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Tools.Build.Run() in C:\Source\My Project\Tools\Build.cs:line 8
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Single(d.AllFrames);
        Assert.Equal(8, d.AllFrames[0].SourceLine);
        Assert.Contains("My Project", d.AllFrames[0].SourceFile, StringComparison.Ordinal);
    }

#endregion

#region Recursion (behavioural regression guards)

    [Fact]
    public void Decode_Direct_UserRecursionDetected()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Worker.Run() in W.cs:line 5
                                at MyApp.Worker.Run() in W.cs:line 6
                                at MyApp.Worker.Run() in W.cs:line 7
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.True(d.HasRecursion);
        Assert.Equal(3, d.RecursionPatterns[0].Depth);
    }

    [Fact]
    public void Decode_Alternating_MutualRecursionDetected()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Alpha.StepA() in A.cs:line 1
                                at MyApp.Beta.StepB() in B.cs:line 1
                                at MyApp.Alpha.StepA() in A.cs:line 2
                                at MyApp.Beta.StepB() in B.cs:line 2
                                at MyApp.Alpha.StepA() in A.cs:line 3
                                at MyApp.Beta.StepB() in B.cs:line 3
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.True(d.HasRecursion);
        Assert.True(d.RecursionPatterns[0].Depth >= 6);
    }

    [Fact]
    public void Decode_AutoMapper_MapTailNotRecursion()
    {
        const string trace = """
                             AutoMapper.AutoMapperMappingException: map
                                at AutoMapper.MappingEngine.Map(ResolutionContext ctx)
                                at AutoMapper.MappingEngine.Map[TSource,TDest](TSource src)
                                at MyApp.Program.Main() in Program.cs:line 1
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.False(d.HasRecursion);
    }

#endregion

#region Parsing / misc

    [Fact]
    public void Decode_Multi_LineExceptionHeaderPreservedBeforeAtLines()
    {
        const string trace = """
                             System.ApplicationException: First line of message.
                             Second line with detail.
                                at MyApp.Foo.Bar() in F.cs:line 2
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Single(d.AllFrames);
        Assert.Contains("First line", d.ExceptionMessage, StringComparison.Ordinal);
        Assert.Contains("Second line", d.ExceptionMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_Fingerprint_IgnoresLineNumberChanges()
    {
        const string a = """
                         System.Exception: x
                            at MyApp.Stable.MethodA() in X.cs:line 10
                            at MyApp.Stable.MethodB() in Y.cs:line 20
                         """;

        const string b = """
                         System.Exception: x
                            at MyApp.Stable.MethodA() in X.cs:line 99
                            at MyApp.Stable.MethodB() in Y.cs:line 200
                         """;

        var fa = DefaultDecoder.Decode(a).Fingerprint;
        var fb = DefaultDecoder.Decode(b).Fingerprint;
        Assert.Equal(fa, fb);
    }

    [Fact]
    public void Decode_No_AtFramesMessageOnly()
    {
        const string trace = """
                             System.TimeoutException: Operation timed out.
                             Additional detail without stack.
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(0, d.TotalFrameCount);
        Assert.Contains("timed out", d.ExceptionMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decode_Lambda_FrameMarkedAsLambda()
    {
        const string trace = """
                             System.Exception: x
                                at MyApp.Program.<>c__DisplayClass0_0.<Main>b__0() in Program.cs:line 12
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Single(d.AllFrames);
        Assert.True(d.AllFrames[0].IsLambda);
    }

    [Fact]
    public void Decode_EllipsisInParameterListStill_ParsesAsFrame()
    {
        const string trace = """
                             System.Exception: x
                                at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(...)
                                at MyApp.C.Done() in C.cs:line 1
                             """;

        var d = DefaultDecoder.Decode(trace);
        Assert.Equal(2, d.TotalFrameCount);
        Assert.Contains("Execute", d.AllFrames[0].FullMethod, StringComparison.Ordinal);
    }

#endregion
}