using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

public sealed class StackTraceDecoderInnerExceptionTests
{
    private const string AutoMapperFormatInner = """
AutoMapper.AutoMapperMappingException: Error mapping types.

Mapping types:
    OrderDto -> Order (MyApp.DTOs.OrderDto -> MyApp.Models.Order)

   at AutoMapper.MappingEngine.Map(ResolutionContext context)
   at AutoMapper.MappingEngine.Map[TSource,TDestination](TSource source)
   at MyApp.Services.OrderService.CreateOrder(OrderDto dto) in OrderService.cs:line 91
   at MyApp.Controllers.OrderController.Post(OrderDto dto) in OrderController.cs:line 44
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(...)

 ---> System.FormatException: The string '01-32-2024' was not recognized as a valid DateTime.
   at System.DateTime.ParseExact(String s, String format, IFormatProvider provider)
   at MyApp.DTOs.OrderDto.get_OrderDate()
   --- End of inner exception stack trace ---
""";

    [Fact]
    public void Decode_AutoMapperWithInner_PicksInnermostUser_ForCrashAndDeepest()
    {
        var trace = new StackTraceDecoder().Decode(AutoMapperFormatInner);
        Assert.False(trace.HasRecursion);
        Assert.Contains("get_OrderDate", trace.LikelyCrashSite?.FullMethod ?? "", StringComparison.Ordinal);
        Assert.Contains("get_OrderDate", trace.DeepestUserFrame?.FullMethod ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_IdenticalSystemFrames_Only_DoesNotFlagRecursion()
    {
        const string sysOnly = """
System.Exception: x
   at System.Console.WriteLine()
   at System.Console.WriteLine()
   at System.Console.WriteLine()
""";
        var trace = new StackTraceDecoder().Decode(sysOnly);
        Assert.False(trace.HasRecursion);
    }
}
