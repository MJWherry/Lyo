import { Counter, Rate, Trend } from "k6/metrics";

export const requestDuration = new Trend("query_duration", true);
export const responseBytes = new Trend("response_bytes", true);
export const requestBytes = new Trend("request_bytes", true);

export const scenarioDuration = new Trend("scenario_duration", true);
export const failedStatusCount = new Counter("failed_status_count");
export const slowResponseCount = new Counter("slow_response_count");
export const successRate = new Rate("scenario_success_rate");
