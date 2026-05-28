using Lyo.Exceptions;

namespace Lyo.Images.Builders;

/// <summary>Builder-flavored extensions for <see cref="IImageDecorationPipeline" />: each method takes a configurator on the matching fluent builder and queues the resulting stage.</summary>
public static class ImageDecorationPipelineBuilderExtensions
{
    /// <summary>Queues an overlay stage configured through an <see cref="OverlayOptionsBuilder" />.</summary>
    public static IImageDecorationPipeline Overlay(this IImageDecorationPipeline pipeline, Stream overlayStream, Action<OverlayOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(pipeline);
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = OverlayOptionsBuilder.New();
        configure(builder);
        return pipeline.Overlay(overlayStream, builder.Build());
    }

    /// <inheritdoc cref="Overlay(IImageDecorationPipeline, Stream, Action{OverlayOptionsBuilder})" />
    public static IImageDecorationPipeline Overlay(this IImageDecorationPipeline pipeline, byte[] overlayBytes, Action<OverlayOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(pipeline);
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = OverlayOptionsBuilder.New();
        configure(builder);
        return pipeline.Overlay(overlayBytes, builder.Build());
    }

    /// <summary>Queues a frame stage configured through a <see cref="FrameOptionsBuilder" />.</summary>
    public static IImageDecorationPipeline AddFrame(this IImageDecorationPipeline pipeline, Action<FrameOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(pipeline);
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = FrameOptionsBuilder.New();
        configure(builder);
        return pipeline.AddFrame(builder.Build());
    }

    /// <summary>Queues a caption stage configured through a <see cref="CaptionOptionsBuilder" />.</summary>
    public static IImageDecorationPipeline AddCaption(this IImageDecorationPipeline pipeline, Action<CaptionOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(pipeline);
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = CaptionOptionsBuilder.New();
        configure(builder);
        return pipeline.AddCaption(builder.Build());
    }

    /// <summary>Queues an outer-padding stage configured through a <see cref="PaddingOptionsBuilder" />.</summary>
    public static IImageDecorationPipeline AddOuterPadding(this IImageDecorationPipeline pipeline, Action<PaddingOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(pipeline);
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = PaddingOptionsBuilder.New();
        configure(builder);
        return pipeline.AddOuterPadding(builder.Build());
    }
}