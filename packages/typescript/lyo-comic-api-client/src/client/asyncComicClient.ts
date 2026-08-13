import type { AsyncApiClient, ApiResponse, ProjectedQueryRes } from "lyo-api-client";
import type { ProjectionQueryReq } from "lyo-query";
import type {
  ComicChapter,
  ComicChapterRes,
  ComicPage,
  ComicPageRes,
  ComicSeriesQuery,
  ComicSeriesRes,
  ComicVolumeRes,
} from "../models/comic.js";
import { comicQueryProjectRoute, type ComicQueryScope } from "../models/query.js";

const PREFIX = "/api/comic";

function enc(id: string): string {
  return encodeURIComponent(id);
}

/**
 * Comic-specific reads (slug, nested lists, tags, files).
 * Generic CRUD / QueryProject live on {@link AsyncApiClient} (`create`, `update`, `deleteById`, `queryProject`).
 */
export interface AsyncComicApiClient {
  getSeries(id: string): Promise<ApiResponse<ComicSeriesRes>>;
  getSeriesBySlug(slug: string): Promise<ApiResponse<ComicSeriesRes>>;
  getSeriesChapters(seriesId: string, language?: string): Promise<ApiResponse<ComicChapter[]>>;
  getSeriesVolumes(seriesId: string): Promise<ApiResponse<ComicVolumeRes[]>>;
  getAllSeriesTags(): Promise<ApiResponse<string[]>>;
  getSeriesTags(seriesId: string): Promise<ApiResponse<string[]>>;
  addSeriesTag(seriesId: string, req: { name: string; tagType?: string; slug?: string | null }): Promise<ApiResponse<unknown>>;
  removeSeriesTag(seriesId: string, tag: string): Promise<ApiResponse<unknown>>;
  searchSeries(query: ComicSeriesQuery): Promise<ApiResponse<ComicSeriesRes[]>>;

  getVolume(id: string): Promise<ApiResponse<ComicVolumeRes>>;
  getVolumeChapters(volumeId: string, language?: string): Promise<ApiResponse<ComicChapterRes[]>>;

  getChapter(id: string): Promise<ApiResponse<ComicChapterRes>>;
  getChapterPages(chapterId: string): Promise<ApiResponse<ComicPage[]>>;

  getPage(id: string): Promise<ApiResponse<ComicPageRes>>;

  queryProjected<TRow = Record<string, unknown>>(
    scope: ComicQueryScope,
    request: ProjectionQueryReq
  ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;

  /** Next-origin path for `<img src>` — not the Comic API host. */
  getFileUrl(fileId: string): string;
}

export function createAsyncComicApiClient(apiClient: AsyncApiClient): AsyncComicApiClient {
  return {
    getSeries(id) {
      return apiClient.request<ComicSeriesRes>({ method: "GET", path: `${PREFIX}/series/${enc(id)}` });
    },
    getSeriesBySlug(slug) {
      return apiClient.request<ComicSeriesRes>({ method: "GET", path: `${PREFIX}/series/slug/${enc(slug)}` });
    },
    getSeriesChapters(seriesId, language) {
      const query = language ? { language } : undefined;
      return apiClient.request<ComicChapter[]>({
        method: "GET",
        path: `${PREFIX}/series/${enc(seriesId)}/chapters`,
        query,
      });
    },
    getSeriesVolumes(seriesId) {
      return apiClient.request<ComicVolumeRes[]>({
        method: "GET",
        path: `${PREFIX}/series/${enc(seriesId)}/volumes`,
      });
    },
    getAllSeriesTags() {
      return apiClient.request<string[]>({ method: "GET", path: `${PREFIX}/series/tags` });
    },
    getSeriesTags(seriesId) {
      return apiClient.request<string[]>({ method: "GET", path: `${PREFIX}/series/${enc(seriesId)}/tags` });
    },
    addSeriesTag(seriesId, req) {
      return apiClient.request({ method: "POST", path: `${PREFIX}/series/${enc(seriesId)}/tags`, body: req });
    },
    removeSeriesTag(seriesId, tag) {
      return apiClient.request({
        method: "DELETE",
        path: `${PREFIX}/series/${enc(seriesId)}/tags/${enc(tag)}`,
      });
    },
    searchSeries(query) {
      return apiClient.request<ComicSeriesRes[], ComicSeriesQuery>({
        method: "POST",
        path: `${PREFIX}/series/search`,
        body: query,
      });
    },

    getVolume(id) {
      return apiClient.request<ComicVolumeRes>({ method: "GET", path: `${PREFIX}/volumes/${enc(id)}` });
    },
    getVolumeChapters(volumeId, language) {
      const query = language ? { language } : undefined;
      return apiClient.request<ComicChapterRes[]>({
        method: "GET",
        path: `${PREFIX}/volumes/${enc(volumeId)}/chapters`,
        query,
      });
    },

    getChapter(id) {
      return apiClient.request<ComicChapterRes>({ method: "GET", path: `${PREFIX}/chapters/${enc(id)}` });
    },
    getChapterPages(chapterId) {
      return apiClient.request<ComicPage[]>({
        method: "GET",
        path: `${PREFIX}/chapters/${enc(chapterId)}/pages`,
      });
    },

    getPage(id) {
      return apiClient.request<ComicPageRes>({ method: "GET", path: `${PREFIX}/pages/${enc(id)}` });
    },

    queryProjected(scope, request) {
      return apiClient.queryProject(comicQueryProjectRoute(scope), request);
    },

    getFileUrl(fileId) {
      return `/api/files/${enc(fileId)}`;
    },
  };
}
