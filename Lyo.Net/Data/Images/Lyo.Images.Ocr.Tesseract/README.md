# Lyo.Images.Ocr.Tesseract

**Tesseract** implementation of **`IOcrEngine`** from **`Lyo.Images.Ocr`**. Calls are **serialized** with an internal lock because native Tesseract instances are not safely concurrent.

## Tesseract setup (step by step)

### 1. Native runtime

**Windows**

1. Restore/build your app so the **Tesseract** NuGet copies **`x64\tesseract50.dll`** (and Leptonica) into your output folder.
2. Run your process from that output folder (or ensure those DLLs are on the loader search path).

**Linux (Debian/Ubuntu-style)**

`libtesseract5` is only the **native library**. Integration tests and **`IOcrEngine`** still need **`eng.traineddata`** on disk — that comes from a **language-data** package, not from `libtesseract5` alone.

1. Update package lists: `sudo apt-get update`
2. Install native libraries:
   - **Tesseract:** `sudo apt-get install -y libtesseract5`
   - **Leptonica** (required by the NuGet interop; package name varies): `sudo apt-get install -y libleptonica6`  
     If that package does not exist on your release: `sudo apt-get install -y libleptonica-dev`  
     **`libleptonica-dev` only installs `libleptonica.so` / `libleptonica.so.*` — it never creates `libleptonica-1.82.0.so`.** You still need the symlink in the subsection below.
3. Install **English traineddata** (pick one):
   - Minimal: `sudo apt-get install -y tesseract-ocr-eng`
   - Or full meta (CLI + common languages): `sudo apt-get install -y tesseract-ocr`
4. Confirm the `.so` exists: `ldconfig -p | grep tesseract` (expect `libtesseract.so.5`)
5. Confirm **`eng.traineddata`** exists (path varies by distro/version):

   ```bash
   find /usr/share/tesseract-ocr -name eng.traineddata 2>/dev/null
   ```

   You should see something like **`/usr/share/tesseract-ocr/5/tessdata/eng.traineddata`**.

#### Linux: `libleptonica-1.82.0.so` / `libtesseract50.so` (NuGet vs distro filenames)

The **charlesw/Tesseract** NuGet asks **`InteropDotNet`** for those exact basenames. Distros ship **`liblept.so.*`**, **`libleptonica.so`**, **`libtesseract.so.5`**, etc. **`apt` does not install `libleptonica-1.82.0.so`.**

**Critical (why `/usr/local/lib` often fails):** on Linux the loader looks next to your built app, under **`x64/`** inside the configuration output folder — for example **`bin/Debug/net10.0/x64/`** — not at global **`ldconfig`** paths. See upstream discussion: [charlesw/tesseract#687](https://github.com/charlesw/tesseract/issues/687).

**Directory layout:** the real test project lives next to the Tesseract project:

- **`Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/`** — this is what **`dotnet test`** builds (output: **`.../bin/Debug/net10.0/`**).

Do **not** confuse it with a nested **`Lyo.Images.Ocr.Tesseract/Lyo.Images.Ocr.Tesseract.Tests/`** folder; that path is not the SDK layout and is often created accidentally (sometimes **`root`**-owned). Remove it with **`sudo rm -rf`** if it appears.

**`libdl.so`:** **`InteropDotNet`** loads the library name **`libdl`**. Glibc only provides **`libdl.so.2`** (often under **`/lib/x86_64-linux-gnu/`**), so **`libdl.so` is missing** unless you add a symlink. The setup script drops **`libdl.so` → libdl.so.2** next to **`$(OutputPath)`** (same folder as your **`*.dll`**), which is one of the paths .NET probes.

**Automatic (Linux):** projects under **`Lyo.Net/Data/Images/`** that set **`PrepareTesseractLinuxNativeLibs`** to **`true`** run **`scripts/setup-linux-tesseract-nuget-libs.sh`** after each **`Build`**, passing **`$(OutputPath)`** (the **`net10.0/`** folder). **`Lyo.Images.Ocr.Tesseract`** and **`Lyo.Images.Ocr.Tesseract.Tests`** opt in. Other host apps can set the same property on their **`.csproj`** if they reference Tesseract and build on Linux.

**Manual / CI-only:** run the script yourself after **`dotnet build`** / **`dotnet publish`** when you do not use that property:

Pass the **TFM output directory** (`**/bin/Debug/net10.0`, not `**/x64`):

```bash
bash Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract/scripts/setup-linux-tesseract-nuget-libs.sh \
  "$PWD/Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/bin/Debug/net10.0"
```

Re-run after a clean build if output was deleted.

Optional: **`--also-system`** uses **`sudo`** to mirror Leptonica/Tesseract under **`/usr/local/lib`**, **`libdl.so`** next to the system **`libdl.so.2`**, and runs **`ldconfig`**. Usually unnecessary if app-local symlinks exist.

Manual equivalent (same idea as the script):

```bash
OUT=/path/to/your/app/bin/Debug/net10.0
ARCH=x86_64-linux-gnu
mkdir -p "$OUT/x64"
ln -sf "$(readlink -f /usr/lib/$ARCH/libleptonica.so 2>/dev/null || readlink -f /usr/lib/$ARCH/liblept.so.5)" \
  "$OUT/x64/libleptonica-1.82.0.so"
ln -sf "$(readlink -f /usr/lib/$ARCH/libtesseract.so.5)" "$OUT/x64/libtesseract50.so"
ln -sf "$(readlink -f /lib/$ARCH/libdl.so.2)" "$OUT/libdl.so"
```

**macOS (Homebrew)**

1. Install: `brew install tesseract`
2. Confirm: `tesseract --version`
3. Confirm traineddata: `ls "$(brew --prefix)/share/tessdata/eng.traineddata"`

### 2. Language data (`traineddata`) — manual / custom layout

1. If you use distro packages (Linux/macOS above), **`TessdataDirectory`** should be the **`tessdata`** folder that contains **`eng.traineddata`** (tests auto-detect common Linux paths and any **`/usr/share/tesseract-ocr/*/tessdata`** directory).
2. Otherwise ship or download **`eng.traineddata`** into a folder you control (other languages = more `*.traineddata` files).
3. Manual download (English **fast** model): [tessdata_fast — eng.traineddata](https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata)
4. Point **`TesseractOcrEngineOptions.TessdataDirectory`** at the **directory that contains** `eng.traineddata`, not the parent of `tessdata`.

### 3. Wire configuration

1. Set **`OcrEngine:DefaultLanguages`** to match your files (e.g. **`eng`**, or **`eng+jpn`** if both `eng.traineddata` and `jpn.traineddata` are present).
2. Set **`OcrEngine:Tesseract:TessdataDirectory`** to the absolute path from step 2.
3. Start the app and exercise **`IOcrEngine.ReadAsync`** once; if tessdata is wrong you typically get engine creation errors referencing missing `*.traineddata`.

### Automated tests

Configuration is loaded once per assembly via **`TesseractOcrTestFixture`** (**`AssemblyFixture`**, same idea as other Postgres-style integration fixtures): **`IConfiguration`** is registered in a **`ServiceCollection`** so config tests resolve it from **`IServiceProvider`**. OCR settings are materialized with **`IConfigurationSection.Get<T>()`** / **`ConfigurationBinder`** (not **`Bind(instance)`**).

**`appsettings.json`** lives next to the test project (committed defaults: integration **off**) with optional **`appsettings.Development.json`** (gitignored repo-wide—same pattern as app hosts).

1. Install **library + language data** as above (`tesseract-ocr-eng` or `tesseract-ocr` on Debian/Ubuntu).
2. Enable OCR integration either way:
   - **Recommended:** set **`OcrTesseractTests:RunIntegration`** to **`true`** in **`appsettings.Development.json`**, or edit **`appsettings.json`** locally (don’t commit `true` if CI must stay off).
   - **Alternate:** **`LYO_RUN_TESSERACT_INTEGRATION=1`** (still supported; overrides/appsettings merge via **`AddEnvironmentVariables`**).
3. Tessdata path resolution (first match wins): **`OcrTesseractTests:TessdataDirectory`**, then **`OcrEngine:Tesseract:TessdataDirectory`**, then output **`tessdata/`**, **`LYO_TESSDATA_DIRECTORY`**, then distro paths under **`/usr/share/tesseract-ocr`**.
4. Run: **`dotnet test Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/`**
5. Native integration tests (**`ReadAsync_*`** with real Tesseract) use **`Assert.SkipUnless` / `Assert.SkipWhen`**: they show as **skipped** (not passed) when integration is off, **`eng.traineddata`** cannot be resolved, or native libraries are missing (**`OCR_NATIVE_LIBRARY_NOT_FOUND`**). The always-on tests (**`ReadAsync_missing_tessdata_*`**, configuration tests) still run.
6. If assertions mention missing tessdata, confirm **`find /usr/share/tesseract-ocr -name eng.traineddata`** — **`ldconfig`** showing **`libtesseract.so.5`** alone is not enough.

## Dependency injection

```csharp
services.AddTesseractOcrEngine(
    shared => shared.DefaultLanguages = "eng",
    tess => tess.TessdataDirectory = "/path/to/tessdata");
```

Or from configuration:

```csharp
services.AddTesseractOcrEngineFromConfiguration(configuration);
```

With **appsettings.json**:

```json
{
  "OcrEngine": {
    "EnableMetrics": false,
    "DefaultLanguages": "eng",
    "DefaultPageSegmentationMode": "Auto",
    "Tesseract": {
      "TessdataDirectory": "/usr/share/tesseract-ocr/5/tessdata"
    }
  }
}
```

If **`OcrEngineOptions`** was already registered (e.g. via **`AddOcrEngineOptionsFromConfiguration`**), use **`AddTesseractOcrEngine`** **without** the `configureShared` delegate so shared options are not registered twice.

## Coordinate space

Word boxes follow **`Lyo.Images.Ocr`** conventions (**Y-up pixel coordinates**). Use **`OcrCoordinateTransforms`** when integrating with PDF points (**`Lyo.Pdf.Ocr`**).
