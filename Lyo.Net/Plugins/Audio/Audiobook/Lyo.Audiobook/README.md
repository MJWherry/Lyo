# Lyo.Audiobook

Domain contracts for a spoken-word catalog. A work may be one audio file, a chapter list, or both. Optional series membership. Credits use an opaque `PersonRef`. `CoverImageRef` and `AudioFileRef` are opaque file refs. Favorite and Tag attach via `AudiobookEntityTypes`. The print book catalog is a separate plugin.

## Features

- **Work.** `SaveWorkAsync` replaces credits, identifiers, and chapters. A null transcript leaves the stored text in place.
- **Identifiers.** `isbn` keeps digits and a trailing X. `asin` and `custom` are trim-only. Unique on `(scheme, normalized value)`. Chapters have none.
- **Series.** Delete clears `SeriesId` and `SeriesIndex` and keeps the works.
- **Chapters.** Unique `(work, number)`. The same number on another work is allowed.
- **Discovery.** Genre trees and `SearchWorksAsync`. `FilterIds` null means no filter. An empty list returns no rows.
- **Transcripts.** Optional plain text on a work or chapter. Work reads omit it. `SearchTranscriptsAsync` uses the stored full-text index.