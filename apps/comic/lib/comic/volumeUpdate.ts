import type { ComicVolumeReq, ComicVolumeRes } from "lyo-comic-api-client";

export function volumeUpdateData(volume: ComicVolumeRes, patch: Partial<ComicVolumeReq> = {}): ComicVolumeReq {
  return {
    seriesId: patch.seriesId ?? volume.seriesId,
    volumeNumber: patch.volumeNumber !== undefined ? patch.volumeNumber : (volume.volumeNumber ?? null),
    title: patch.title !== undefined ? patch.title : (volume.title ?? null),
    coverImageRef: patch.coverImageRef !== undefined ? patch.coverImageRef : (volume.coverImageRef ?? null),
    publishedDate: patch.publishedDate !== undefined ? patch.publishedDate : (volume.publishedDate ?? null),
  };
}
