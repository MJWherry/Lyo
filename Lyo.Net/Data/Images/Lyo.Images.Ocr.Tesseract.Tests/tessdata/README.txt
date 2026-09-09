Place eng.traineddata here for integration tests, or install system tessdata and set LYO_TESSDATA_DIRECTORY.

Download (fast English model):
https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata

Enable integration (pick one):
- appsettings.Development.json: "OcrTesseractTests": { "RunIntegration": true }
- Or: LYO_RUN_TESSERACT_INTEGRATION=1 dotnet test Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/

Linux: apt install libtesseract5 tesseract-ocr-eng libleptonica-dev
NuGet needs libleptonica-1.82.0.so — symlink from distro lib, see Lyo.Images.Ocr.Tesseract README.

Japanese OCR tests (optional): install the same tessdata folder as English plus jpn, e.g.
  apt install tesseract-ocr-jpn
or copy jpn.traineddata next to eng.traineddata from:
  https://github.com/tesseract-ocr/tessdata_fast/raw/main/jpn.traineddata
Rendering tests also need a Japanese-capable font (e.g. fonts-noto-cjk or fonts-ipafont).
