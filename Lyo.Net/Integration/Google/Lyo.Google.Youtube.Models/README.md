# Lyo.Google.Youtube.Models

DTOs for a YouTube video, channel, and playlist item, plus `YoutubeVideoLink` (video id, channel id, optional playlist id). No HTTP types and no project references. `Lyo.Video` stores the link. A host that calls the Data API uses `Lyo.Google.Youtube.Client` and passes these models into the video store.

## Features

- **Link.** `YoutubeVideoLink` is the only YouTube shape a catalog row stores.
- **Responses.** `YoutubeVideo`, `YoutubeChannel`, and `YoutubePlaylistItem` are API results, not catalog columns.