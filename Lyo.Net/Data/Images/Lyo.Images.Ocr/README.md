# Lyo.Images.Ocr

Engine-agnostic **OCR contracts** for Lyo: **`IOcrEngine`**, request/response models, **Y-up pixel bounding boxes** (aligned with [`BoundingBox2D`](../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs)), coordinate helpers, and shared **`OcrEngineOptions`**.

Implementations (e.g. **`Lyo.Images.Ocr.Tesseract`**) register **`IOcrEngine`**. This package only registers cross-provider options via **`AddOcrEngineOptions`** / **`AddOcrEngineOptionsFromConfiguration`**.

## Bounding boxes

- **`OcrWord.BoundingBoxPixels`** uses **Y-up** coordinates: origin at the **bottom-left** of the image, so **`Top` &gt; `Bottom`** and **`Height = Top - Bottom`** matches [`BoundingBox2D`](../../Core/Common/Lyo.Common/Records/BoundingBox2D.cs).
- Use **`OcrCoordinateTransforms.FromTopLeftDownwardRect`** to convert typical top-left raster rects (e.g. Tesseract) into this form.
- For PDF overlay with a rendered page, use **`OcrCoordinateTransforms.MapPixelBoxToPdfPoints`** (see **`Lyo.Pdf.Ocr`**).

## Configuration

```json
{
  "OcrEngine": {
    "EnableMetrics": false,
    "DefaultLanguages": "eng",
    "DefaultPageSegmentationMode": "Auto"
  }
}
```

## Related packages

| Package | Role |
|---------|------|
| **`Lyo.Images.Ocr.Tesseract`** | Local OCR via Tesseract |
| **`Lyo.Pdf.Rendering`** | Rasterize PDF pages for OCR |
| **`Lyo.Pdf.Ocr`** | Combine rasterization + OCR + PDF coordinate mapping |
