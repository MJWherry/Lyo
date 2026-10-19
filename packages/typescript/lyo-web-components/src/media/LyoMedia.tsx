"use client";

import Typography from "@mui/material/Typography";
import { resolveElementId } from "../provider/elementId.js";
import { htmlCaptionKind, imageSrc, normalizeMediaSrc, resolveCaptions, type MediaCaptionTrack } from "./media.js";

function CaptionTracks({ captions }: { captions?: readonly MediaCaptionTrack[] | null }) {
  return resolveCaptions(captions).map((track) => (
    <track
      key={`${track.src}:${track.language ?? ""}:${track.kind}`}
      src={track.src}
      kind={htmlCaptionKind(track.kind)}
      label={track.label}
      srcLang={track.language}
      default={track.default}
    />
  ));
}

export type LyoImageProps = {
  src?: string | null;
  /** Used only when `src` is blank. */
  poster?: string | null;
  /** Empty marks a decorative image. */
  alt?: string;
  elementId?: string;
};

/** Image viewer. A blank src and poster render “No image.” */
export function LyoImage({ src, poster, alt = "", elementId }: LyoImageProps) {
  const resolved = imageSrc(src, poster);
  if (!resolved) return <Typography variant="body2" color="text.secondary">No image.</Typography>;
  return <img id={resolveElementId(elementId, "lyo-image")} src={resolved} alt={alt} style={{ width: "100%", objectFit: "contain" }} />;
}

export type LyoAudioProps = {
  src?: string | null;
  /** Browser controls. Default true. */
  controls?: boolean;
  captions?: readonly MediaCaptionTrack[] | null;
  elementId?: string;
};

/** Audio viewer. A blank src renders “No audio.” */
export function LyoAudio({ src, controls = true, captions, elementId }: LyoAudioProps) {
  const resolved = normalizeMediaSrc(src);
  if (!resolved) return <Typography variant="body2" color="text.secondary">No audio.</Typography>;
  return (
    <audio id={resolveElementId(elementId, "lyo-audio")} src={resolved} controls={controls} preload="metadata" style={{ width: "100%" }}>
      <CaptionTracks captions={captions} />
    </audio>
  );
}

export type LyoVideoProps = {
  src?: string | null;
  /** Poster frame. Applied only when `src` is present. */
  poster?: string | null;
  /** Browser controls. Default true. */
  controls?: boolean;
  captions?: readonly MediaCaptionTrack[] | null;
  elementId?: string;
};

/** Video viewer. A blank src renders “No video.” and ignores the poster. */
export function LyoVideo({ src, poster, controls = true, captions, elementId }: LyoVideoProps) {
  const resolved = normalizeMediaSrc(src);
  if (!resolved) return <Typography variant="body2" color="text.secondary">No video.</Typography>;
  return (
    <video
      id={resolveElementId(elementId, "lyo-video")}
      src={resolved}
      poster={normalizeMediaSrc(poster) ?? undefined}
      controls={controls}
      preload="metadata"
      style={{ width: "100%" }}
    >
      <CaptionTracks captions={captions} />
    </video>
  );
}
