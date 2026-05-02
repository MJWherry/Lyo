using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Inbox;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

public sealed class ErrorOccurrenceMapperTests
{
    [Fact]
    public void FromDiagnosticContext_TruncatesMessage()
    {
        var decoder = new StackTraceDecoder();
        var classifier = new ExceptionClassifier();
        var builder = new DiagnosticContextBuilder(decoder, classifier);
        var ex = new Exception(new string('x', 100));
        var ctx = builder.Build(ex, RequestMetadata.Empty);
        var record = ErrorOccurrenceMapper.FromDiagnosticContext(ctx, null, null, maxExceptionMessageLength: 20);
        Assert.NotNull(record.ExceptionMessage);
        Assert.Equal(20, record.ExceptionMessage!.Length);
    }

    [Theory]
    [InlineData(ExceptionSeverity.Low, ExceptionSeverity.High, false)]
    [InlineData(ExceptionSeverity.High, ExceptionSeverity.Low, true)]
    [InlineData(ExceptionSeverity.High, ExceptionSeverity.High, true)]
    public void MeetsMinimumSeverity_Reflects_Order(ExceptionSeverity sev, ExceptionSeverity min, bool expected)
        => Assert.Equal(expected, ErrorOccurrenceMapper.MeetsMinimumSeverity(sev, min));
}
