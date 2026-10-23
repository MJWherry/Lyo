# Lyo.Music

Domain contracts for a release-centric music catalog. A `MusicRelease` is one edition. An optional `MusicReleaseGroup` groups editions of the same album. Credits use an opaque
`PersonRef`. `AudioFileRef` and `CoverImageRef` are opaque file refs. Favorite and Tag attach via `MusicEntityTypes`.

## Features

- **Release.** `SaveReleaseAsync` upserts the edition and replaces credits, identifiers, and tracks. `PrimaryArtist` is the Artist or Featured credit with the lowest ordinal.
- **Release group.** Delete clears `ReleaseGroupId` and keeps releases.
- **Identifiers.** `MusicIdentifierSchemes.Normalize` strips non-digits from barcodes. Unique `(scheme, normalized value)` is enforced by the store.
- **Tracks.** Unique `(release, disc, track number)`. ISRC is unique when set. Optional `RecordingId` points at a shared `MusicRecording`.
- **Discovery.** Genre trees, formats, and `SearchReleasesAsync`. `FilterIds` null means no filter. An empty list returns no rows.
- **Lyrics.** Plain text on a track. Release reads omit it. `SearchLyricsAsync` uses the stored full-text index. `SaveTrackLyricsAsync` replaces one track's lyrics.