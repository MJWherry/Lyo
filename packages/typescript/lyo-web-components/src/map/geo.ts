import { geoAlbers, geoAlbersUsa, geoPath, type GeoProjection } from "d3-geo";
import type { Feature, FeatureCollection, GeoJsonProperties, Geometry } from "geojson";
import { feature } from "topojson-client";
import type { GeometryCollection, Topology } from "topojson-specification";
import countiesAtlas from "./geo/counties-10m.json";
import statesAtlas from "./geo/states-10m.json";

export const MAP_WIDTH = 960;
export const MAP_HEIGHT = 600;

export type MapPath = {
  fips: string;
  d: string;
};

type Atlas = Topology<{
  states: GeometryCollection;
  counties: GeometryCollection;
}>;

type MapFeature = Feature<Geometry, GeoJsonProperties>;

const PAD = 8;
const statesTopology = statesAtlas as unknown as Atlas;
const countiesTopology = countiesAtlas as unknown as Atlas;

let nation: MapPath[] | undefined;
let countyFeatures: MapFeature[] | undefined;
const countyCache = new Map<string, MapPath[]>();

/** State outlines from us-atlas, on an Albers USA projection, including the Alaska and Hawaii insets. */
export function nationPaths(): MapPath[] {
  nation ??= draw(featuresOf(statesTopology, "states"), geoAlbersUsa(), 2);
  return nation;
}

/** County outlines for one state, fitted to that state's own Albers projection. */
export function countyPaths(stateFips: string): MapPath[] {
  const cached = countyCache.get(stateFips);
  if (cached) return cached;
  const features = counties().filter((item) => fipsOf(item.id, 5).startsWith(stateFips));
  const drawn = features.length === 0 ? [] : draw(features, geoAlbers(), 5);
  countyCache.set(stateFips, drawn);
  return drawn;
}

function counties(): MapFeature[] {
  countyFeatures ??= featuresOf(countiesTopology, "counties");
  return countyFeatures;
}

function featuresOf(topology: Atlas, object: "states" | "counties"): MapFeature[] {
  const collection = feature(topology, topology.objects[object]) as FeatureCollection<Geometry, GeoJsonProperties>;
  return collection.features;
}

function draw(features: MapFeature[], projection: GeoProjection, fipsWidth: number): MapPath[] {
  const collection: FeatureCollection<Geometry, GeoJsonProperties> = { type: "FeatureCollection", features };
  projection.fitExtent(
    [
      [PAD, PAD],
      [MAP_WIDTH - PAD, MAP_HEIGHT - PAD],
    ],
    collection,
  );
  const path = geoPath(projection);
  return features.map((item) => ({ fips: fipsOf(item.id, fipsWidth), d: path(item) ?? "" }));
}

function fipsOf(id: string | number | undefined, width: number): string {
  return String(id ?? "").padStart(width, "0");
}
