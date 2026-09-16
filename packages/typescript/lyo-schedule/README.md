# lyo-schedule

Edits a `ScheduleDefinition` (set times, interval, one-shot, cron) and previews occurrences on `LyoCalendar`. There is no HTTP client. Job schedules stay in `lyo-job-components`.

```tsx
import {ScheduleEditor} from "lyo-schedule";

<ScheduleEditor value={{type: "Interval", intervalMinutes: 1440, executeAt: "2026-03-01T00:00:00.000Z"}} />
```

A one-day interval from the first of a month yields one hit per remaining day in that month.
