export const MediaCaptionKinds = ["subtitles", "captions", "descriptions", "chapters", "metadata"] as const;

/** HTML `<track>` kind. */
export type MediaCaptionKind = (typeof MediaCaptionKinds)[number];

/** One timed-text file shown as an HTML `<track>`. */
export type MediaCaptionTrack = {
  src: string;
  label?: string;
  language?: string;
  kind?: MediaCaptionKind;
  default?: boolean;
};

/** Trims a viewer URL. Null, empty, and whitespace are absent. */
export function normalizeMediaSrc(src: string | null | undefined): string | null {
  if (src == null || src.trim() === "") return null;
  return src.trim();
}

/** Image URL: `src` when present, otherwise `poster`. Both blank means nothing to show. */
export function imageSrc(src: string | null | undefined, poster: string | null | undefined): string | null {
  return normalizeMediaSrc(src) ?? normalizeMediaSrc(poster);
}

/**
 * Drops tracks with a blank src, trims the rest, and keeps a single default.
 * A missing list is empty. Kind defaults to subtitles.
 */
export function resolveCaptions(tracks: readonly MediaCaptionTrack[] | null | undefined): MediaCaptionTrack[] {
  if (!tracks || tracks.length === 0) return [];
  const resolved: MediaCaptionTrack[] = [];
  let hasDefault = false;
  for (const track of tracks) {
    const src = normalizeMediaSrc(track.src);
    if (!src) continue;
    const isDefault = track.default === true && !hasDefault;
    if (isDefault) hasDefault = true;
    resolved.push({
      src,
      label: track.label,
      language: track.language,
      kind: track.kind ?? "subtitles",
      default: isDefault,
    });
  }
  return resolved;
}

/** HTML `kind` token. A missing kind is subtitles. */
export function htmlCaptionKind(kind: MediaCaptionKind | null | undefined): MediaCaptionKind {
  return kind ?? "subtitles";
}
