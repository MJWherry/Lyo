namespace Lyo.Images.OpenCv;

/// <summary>OpenCV inpaint algorithm choice (maps to OpenCvSharp <c>InpaintTypes</c>).</summary>
public enum OpenCvInpaintAlgorithm
{
    /// <summary>Fast Marching Method (typically good for small holes).</summary>
    Telea = 0,

    /// <summary>Navier–Stokes fluid-dynamics inpaint (can look different from Telea).</summary>
    NavierStokes = 1
}