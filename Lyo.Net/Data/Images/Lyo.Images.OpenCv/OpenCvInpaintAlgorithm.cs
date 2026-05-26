namespace Lyo.Images.OpenCv;

/// <summary>OpenCV inpaint algorithm selection (maps to OpenCvSharp <c>InpaintTypes</c>).</summary>
public enum OpenCvInpaintAlgorithm
{
    /// <summary>Fast Marching Method (often good for small holes).</summary>
    Telea = 0,

    /// <summary>Navier–Stokes based fluid dynamics inpaint (can differ visually from Telea).</summary>
    NavierStokes = 1
}