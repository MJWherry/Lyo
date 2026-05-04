using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

/// <summary>Rich example stack traces and expectations for <see cref="StackTraceDecoder" /> behaviour (classification, crash site, recursion, parsing).</summary>
public sealed class StackTraceDecoderScenarioTests
{
    private static readonly StackTraceDecoder DefaultDecoder = new();
    private static readonly StackTraceDecoder StrictDecoder = new(new StackTraceDecoderOptions { RestrictUserCodeToListedPrefixes = true });

    #region Classification — BCL / Microsoft platform, ExtraSystemPrefixes, test framework

    [Fact]
    public void Scenario_Pure_System_Trace_Has_No_User_Frames()
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
    public void Scenario_Default_Decoder_Treats_Unconfigured_NuGet_Style_Namespace_As_UserCode()
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
    public void Scenario_ExtraSystemPrefixes_Classifies_Listed_Vendors_As_ThirdParty()
    {
        const string trace = """
System.Exception: x
   at FluentValidation.AbstractValidator.Validate(ValidationContext context)
   at Npgsql.Internal.NpgsqlConnector.ReadMessage()
   at MyApp.Data.Orders.Load() in Orders.cs:line 88
""";
        var d = new StackTraceDecoder(new StackTraceDecoderOptions {
            ExtraSystemPrefixes = ["FluentValidation.", "Npgsql."]
        }).Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[1].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[2].Category);
        Assert.Single(d.UserFrames);
    }

    [Fact]
    public void Scenario_EF_Core_And_SqlClient_Classified_As_ThirdParty()
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
    public void Scenario_OperationCanceled_Classified_As_System_Innermost_User_Still_Selected_When_Present()
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
    public void Scenario_Xunit_Frame_Classified_As_TestFramework()
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
    public void Scenario_NUnit_Frame_Classified_As_TestFramework()
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
    public void Scenario_Microsoft_AspNetCore_Mvc_Classified_As_ThirdParty()
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
    public void Scenario_UserPrefix_Overrides_BuiltIn_System_List()
    {
        const string trace = """
System.Exception: x
   at Microsoft.Internal.MyProduct.Core.Service.Run() in S.cs:line 1
""";
        var d = new StackTraceDecoder(new StackTraceDecoderOptions { UserCodePrefixes = ["Microsoft.Internal.MyProduct."] }).Decode(trace);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[0].Category);
    }

    #endregion

    #region Strict user-code mode

    [Fact]
    public void Scenario_Strict_Empty_UserPrefixes_Everything_NonTest_Is_ThirdParty_Including_MyApp()
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
    public void Scenario_Strict_With_MyApp_Prefix_Only_MyApp_Is_User()
    {
        const string trace = """
System.Exception: x
   at Contoso.Sdk.Client.Call() in C.cs:line 1
   at MyApp.Services.Orchestrator.Run() in O.cs:line 5
""";
        var d = new StackTraceDecoder(new StackTraceDecoderOptions {
            UserCodePrefixes = ["MyApp."],
            RestrictUserCodeToListedPrefixes = true
        }).Decode(trace);
        Assert.Equal(FrameCategory.SystemOrThirdParty, d.AllFrames[0].Category);
        Assert.Equal(FrameCategory.UserCode, d.AllFrames[1].Category);
        Assert.Equal("O.cs:5", d.UserFrames[0].LocationSummary);
    }

    #endregion

    #region Frame order and user frame list

    [Fact]
    public void Scenario_UserFrames_Are_Innermost_First_Matching_AllFrames_Order()
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

    #region Inner exceptions and crash / deepest

    [Fact]
    public void Scenario_Inner_Exception_Wins_For_LikelyCrashSite_And_Deepest()
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
    public void Scenario_Inner_Has_No_User_Frame_Fallback_To_Outer_User()
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
    public void Scenario_Nested_Inners_Deepest_Inner_User_Wins()
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
    public void Scenario_StripAsyncNoise_Removes_Compiler_Generated_Frame_From_User_And_Groups()
    {
        const string trace = """
System.Exception: x
   at MyApp.Worker+<FetchOrder>d__3.MoveNext()
   at MyApp.Services.OrderService.Place() in OrderService.cs:line 20
""";
        var stripped = new StackTraceDecoder(new StackTraceDecoderOptions { StripAsyncNoise = true }).Decode(trace);
        var full = DefaultDecoder.Decode(trace);

        Assert.Equal(2, stripped.TotalFrameCount);
        Assert.Equal(2, full.UserFrameCount);
        Assert.Equal(1, stripped.UserFrameCount);
        Assert.DoesNotContain("MoveNext", stripped.UserFrames[0].FullMethod, StringComparison.Ordinal);
        Assert.Contains("OrderService", stripped.UserFrames[0].FullMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Scenario_RecursionThreshold_Two_Flags_Pair_Of_Identical_User_Frames()
    {
        const string trace = """
System.Exception: x
   at MyApp.Idempotent.Retry() in R.cs:line 1
   at MyApp.Idempotent.Retry() in R.cs:line 2
""";
        var d = new StackTraceDecoder(new StackTraceDecoderOptions { RecursionThreshold = 2 }).Decode(trace);
        Assert.True(d.HasRecursion);
        Assert.Equal(2, d.RecursionPatterns[0].Depth);
    }

    #endregion

    #region More frameworks and edge cases

    [Fact]
    public void Scenario_MSTests_Assert_Classified_As_TestFramework()
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
    public void Scenario_Refit_Without_Config_Is_UserCode()
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
    public void Scenario_UserNamespaces_Are_Distinct_And_Sorted()
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
    public void Scenario_Inner_Exception_With_No_Inner_Stack_Frames_Fallback_Crash_To_Outer_User()
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
    public void Scenario_Generic_Type_Arity_In_Method_String_Still_User_Code()
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
    public void Scenario_Source_File_With_Spaces_In_Path_Parses_Line()
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
    public void Scenario_Direct_User_Recursion_Detected()
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
    public void Scenario_Alternating_Mutual_Recursion_Detected()
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
    public void Scenario_AutoMapper_Map_Tail_Not_Recursion()
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
    public void Scenario_Multi_Line_Exception_Header_Preserved_Before_At_Lines()
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
    public void Scenario_Fingerprint_Ignores_Line_Number_Changes()
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
    public void Scenario_No_At_Frames_Message_Only()
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
    public void Scenario_Lambda_Frame_Marked_As_Lambda()
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
    public void Scenario_Ellipsis_In_Parameter_List_Still_Parses_As_Frame()
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
