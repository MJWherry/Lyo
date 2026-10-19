import { describe, expect, it } from "vitest";
import { htmlCaptionKind, imageSrc, normalizeMediaSrc, resolveCaptions } from "./media.js";

describe("normalizeMediaSrc", () => {
  it("treats blank sources as absent and trims the rest", () => {
    expect(normalizeMediaSrc(null)).toBeNull();
    expect(normalizeMediaSrc("")).toBeNull();
    expect(normalizeMediaSrc("  ")).toBeNull();
    expect(normalizeMediaSrc(" clip.mp4 ")).toBe("clip.mp4");
  });
});

describe("imageSrc", () => {
  it("uses the poster only when src is blank", () => {
    expect(imageSrc(null, "cover.png")).toBe("cover.png");
    expect(imageSrc("  ", "  cover.png ")).toBe("cover.png");
    expect(imageSrc(" photo.png ", "cover.png")).toBe("photo.png");
    expect(imageSrc(null, "  ")).toBeNull();
  });
});

describe("resolveCaptions", () => {
  it("drops a blank src and trims the rest", () => {
    const tracks = resolveCaptions([
      { src: "  " },
      { src: "  subs.vtt ", label: "English", language: "en" },
    ]);
    expect(tracks).toEqual([{ src: "subs.vtt", label: "English", language: "en", kind: "subtitles", default: false }]);
  });

  it("returns no tracks for a missing list", () => {
    expect(resolveCaptions(null)).toEqual([]);
    expect(resolveCaptions(undefined)).toEqual([]);
  });

  it("keeps only the first default", () => {
    const tracks = resolveCaptions([
      { src: "a.vtt", default: true },
      { src: "b.vtt", default: true },
      { src: "c.vtt" },
    ]);
    expect(tracks.map((track) => track.default)).toEqual([true, false, false]);
  });
});

describe("htmlCaptionKind", () => {
  it.each([
    ["subtitles", "subtitles"],
    ["captions", "captions"],
    ["descriptions", "descriptions"],
    ["chapters", "chapters"],
    ["metadata", "metadata"],
  ] as const)("maps %s", (kind, html) => {
    expect(htmlCaptionKind(kind)).toBe(html);
  });

  it("defaults a missing kind to subtitles", () => {
    expect(htmlCaptionKind(undefined)).toBe("subtitles");
  });
});
