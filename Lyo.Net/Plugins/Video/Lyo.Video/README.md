# Lyo.Video

Domain contracts for a video catalog. A movie is a `VideoTitle` with no seasons. A series is a title with seasons and episodes. Credits use an opaque `PersonRef`. `CoverImageRef`
and `VideoFileRef` are opaque file refs. An optional `YoutubeVideoLink` from `Lyo.Google.Youtube.Models` can sit on a movie or an episode. The store does not call the YouTube
client. Favorite and Tag attach via `VideoEntityTypes`.

## Features

- **Title.** `SaveTitleAsync` replaces credits, identifiers, the YouTube link, seasons, and episodes. A movie that includes seasons is rejected.
- **Identifiers.** `imdb`, `tmdb`, `tvdb`, `eidr`, and `custom`. Unique per owner table. There is no youtube identifier scheme.
- **Discovery.** Genre trees, alternate titles, and `SearchTitlesAsync`. `FilterIds` null means no filter. An empty list returns no rows.
- **Reuse.** `VideoCollection` delete clears `CollectionId` and keeps titles. One primary `VideoCut` per movie.
- **Transcripts.** Optional plain text on a movie or episode. Title reads omit it. `SearchTranscriptsAsync` uses the stored full-text index.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Google.Youtube.Models` (direct, lyo)