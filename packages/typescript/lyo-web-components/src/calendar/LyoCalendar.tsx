"use client";

import { useMemo, useState, type ReactNode } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import ButtonGroup from "@mui/material/ButtonGroup";
import Chip from "@mui/material/Chip";
import Autocomplete from "@mui/material/Autocomplete";
import IconButton from "@mui/material/IconButton";
import Dialog from "@mui/material/Dialog";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Paper from "@mui/material/Paper";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import SearchIcon from "@mui/icons-material/Search";
import { hueColor } from "../display/chips.js";
import {
  calendarTimeZones,
  formatCalendarClock,
  formatCalendarOffset,
  monthRows,
  placeDayEvents,
  withTimeZone,
  type CalendarDay,
  type CalendarEntityRef,
  type CalendarEntry,
  type CalendarModel,
  type DayBlock,
  type MonthRow,
} from "./calendar.js";

type ViewMode = "month" | "week" | "day";
type Density = "hourly" | "list";

const WEEKDAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

export type LyoCalendarProps = {
  calendar: CalendarModel | null;
  timeZone?: string;
  onTimeZoneChange?: (timeZone: string) => void;
  use24Hour?: boolean;
  onUse24HourChange?: (use24Hour: boolean) => void;
  onVisibleMonthChange?: (monthStart: string) => void;
  categoryHues?: Record<string, number>;
  renderEntry?: (entry: CalendarEntry) => ReactNode;
  renderDetail?: (entry: CalendarEntry) => ReactNode;
  renderRef?: (entity: CalendarEntityRef) => ReactNode;
};

/** Month, week, and day grid. The magnifying glass opens that day. An entry opens in a dialog. A muted entry is gray. */
export function LyoCalendar({
  calendar,
  timeZone,
  onTimeZoneChange,
  use24Hour = true,
  onUse24HourChange,
  onVisibleMonthChange,
  categoryHues,
  renderEntry,
  renderDetail,
  renderRef,
}: LyoCalendarProps) {
  const [view, setView] = useState<ViewMode>("month");
  const [density, setDensity] = useState<Density>("hourly");
  const [anchor, setAnchor] = useState<string | null>(null);
  const [peek, setPeek] = useState<string | null>(null);
  const [selected, setSelected] = useState<CalendarEntry | null>(null);
  const zone = timeZone || calendar?.timeZone || "UTC";
  const shown = useMemo(() => (calendar && calendar.timeZone !== zone ? withTimeZone(calendar, zone) : calendar), [calendar, zone]);
  const zones = useMemo(() => {
    const known = calendarTimeZones();
    return known.includes(zone) ? known : [zone, ...known];
  }, [zone]);
  const focus = anchor ?? middleDate(shown);
  const days = visibleDays(shown, view, focus);
  const headers = headerLabels(days, view);
  const columns = view === "day" ? 1 : 7;

  const shift = (direction: number) => {
    if (view === "month") {
      const current = middleDate(shown);
      if (current) onVisibleMonthChange?.(shiftMonth(current, direction));
      return;
    }
    if (!focus) return;
    const next = addDays(focus, view === "week" ? 7 * direction : direction);
    setAnchor(next);
    onVisibleMonthChange?.(shiftMonth(next, 0));
  };

  const openDay = (date: string) => {
    setAnchor(date);
    setView("day");
    onVisibleMonthChange?.(shiftMonth(date, 0));
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1, alignItems: "center" }}>
        <IconButton aria-label="Previous" onClick={() => shift(-1)} size="small">
          <ChevronLeftIcon />
        </IconButton>
        <Typography variant="subtitle1">{rangeTitle(days, view, shown)}</Typography>
        <IconButton aria-label="Next" onClick={() => shift(1)} size="small">
          <ChevronRightIcon />
        </IconButton>
        <ButtonGroup size="small" variant="outlined">
          {(["month", "week", "day"] as const).map((mode) => (
            <Button key={mode} variant={view === mode ? "contained" : "outlined"} onClick={() => setView(mode)}>
              {mode[0]!.toUpperCase() + mode.slice(1)}
            </Button>
          ))}
        </ButtonGroup>
        {view !== "month" ? (
          <ButtonGroup size="small" variant="outlined">
            <Button variant={density === "hourly" ? "contained" : "outlined"} onClick={() => setDensity("hourly")}>
              Hourly
            </Button>
            <Button variant={density === "list" ? "contained" : "outlined"} onClick={() => setDensity("list")}>
              List
            </Button>
          </ButtonGroup>
        ) : null}
        <Autocomplete
          size="small"
          sx={{ minWidth: 280 }}
          options={zones}
          value={zone}
          disableClearable
          onChange={(_, value) => onTimeZoneChange?.(value)}
          renderInput={(params) => <TextField {...params} label="Time zone" size="small" />}
        />
        <Typography variant="caption">
          {zone} ({formatCalendarOffset(zone, new Date())})
        </Typography>
        <Switch checked={use24Hour} onChange={(_, checked) => onUse24HourChange?.(checked)} />
        <Typography variant="caption">{use24Hour ? "24-hour" : "12-hour"}</Typography>
      </Box>
      <Dialog open={selected != null} onClose={() => setSelected(null)} fullWidth maxWidth="xs">
        {selected ? (
          <>
            <DialogTitle sx={{ display: "flex", alignItems: "flex-start", gap: 1 }}>
              <Box sx={{ flex: 1, minWidth: 0 }}>{renderDetail ? null : selected.title}</Box>
              <IconButton aria-label="Close event" size="small" onClick={() => setSelected(null)}>
                <CloseIcon fontSize="small" />
              </IconButton>
            </DialogTitle>
            <DialogContent>
              {renderDetail ? (
                renderDetail(selected)
              ) : (
                <>
                  <Typography variant="caption" color="text.secondary">
                    {clockLabel(selected, zone, use24Hour)}
                  </Typography>
                  {selected.description ? (
                    <Typography variant="body2" sx={{ overflowWrap: "anywhere", whiteSpace: "pre-wrap" }}>
                      {selected.description}
                    </Typography>
                  ) : null}
                  {selected.muted ? <Typography variant="body2">This time is inside a blackout.</Typography> : null}
                  <RefList entry={selected} hue={hueOf(selected, categoryHues)} renderRef={renderRef} />
                </>
              )}
            </DialogContent>
          </>
        ) : null}
      </Dialog>
      {shown == null && view === "month" ? (
        <Typography variant="body2" color="text.secondary">
          No calendar to show.
        </Typography>
      ) : view !== "month" && density === "hourly" ? (
        <TimeGrid
          days={days}
          zone={zone}
          use24Hour={use24Hour}
          categoryHues={categoryHues}
          selected={selected}
          onOpenDay={openDay}
          onOpenEntry={setSelected}
        />
      ) : (
        <Box sx={{ display: "grid", gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))`, gap: 0.5 }}>
          {headers.map((header) => (
            <Typography key={`${header}-${days[0]?.date ?? "h"}`} variant="caption" align="center">
              {header}
            </Typography>
          ))}
          {days.map((day) => (
            <MonthCell
              key={day.date}
              day={day}
              zone={zone}
              use24Hour={use24Hour}
              grouped={view === "month"}
              categoryHues={categoryHues}
              renderEntry={renderEntry}
              selected={selected}
              peekOpen={peek === day.date}
              onTogglePeek={() => setPeek((current) => (current === day.date ? null : day.date))}
              onOpenDay={() => openDay(day.date)}
              onOpenEntry={setSelected}
            />
          ))}
        </Box>
      )}
    </Box>
  );
}

function MonthCell({
  day,
  zone,
  use24Hour,
  grouped,
  categoryHues,
  renderEntry,
  selected,
  peekOpen,
  onTogglePeek,
  onOpenDay,
  onOpenEntry,
}: {
  day: CalendarDay;
  zone: string;
  use24Hour: boolean;
  grouped: boolean;
  categoryHues?: Record<string, number>;
  renderEntry?: (entry: CalendarEntry) => ReactNode;
  selected: CalendarEntry | null;
  peekOpen: boolean;
  onTogglePeek: () => void;
  onOpenDay: () => void;
  onOpenEntry: (entry: CalendarEntry) => void;
}) {
  const summary = grouped ? monthRows(day.entries, day.date, zone) : null;
  const shown = summary ? summary.rows.flatMap((row) => row.indexes.map((index) => day.entries[index]).filter((entry): entry is CalendarEntry => entry != null)) : day.entries;
  return (
    <Paper
      variant="outlined"
      onClick={onOpenDay}
      sx={{
        position: "relative",
        p: 0.5,
        minHeight: grouped ? 112 : 160,
        minWidth: 0,
        cursor: "pointer",
        overflow: "visible",
        zIndex: peekOpen ? 20 : undefined,
      }}
    >
      <Typography variant="caption">{Number(day.date.slice(8))}</Typography>
      {grouped ? (
        <IconButton
          aria-label={`Show ${day.date}`}
          size="small"
          onClick={(event) => {
            event.stopPropagation();
            onTogglePeek();
          }}
          sx={{ position: "absolute", top: 0, right: 0, zIndex: 2 }}
        >
          <SearchIcon fontSize="small" />
        </IconButton>
      ) : null}
      <Box sx={{ display: "flex", flexDirection: "column", gap: 0.25, maxHeight: grouped ? 88 : 520, overflow: grouped ? "hidden" : "auto" }}>
        {summary
          ? summary.rows.map((row) => {
              const entry = day.entries[row.indexes[0]!];
              if (!entry) return null;
              if (row.indexes.length === 1) {
                return renderEntry ? (
                  <Box key={`${entry.key}:${entry.start}`} onClick={(event) => event.stopPropagation()} sx={{ minWidth: 0, overflow: "hidden" }}>
                    {renderEntry(entry)}
                  </Box>
                ) : (
                  <EventButton
                    key={`${entry.key}:${entry.start}`}
                    entry={entry}
                    zone={zone}
                    use24Hour={use24Hour}
                    hue={hueOf(entry, categoryHues)}
                    selected={isSelected(selected, entry)}
                    onOpen={() => onOpenEntry(entry)}
                  />
                );
              }
              const label = groupLabel(row, day, use24Hour);
              const muted = row.indexes.every((index) => day.entries[index]?.muted);
              return (
                <Box
                  key={`${row.allDay ? "all" : row.hour}`}
                  component="button"
                  type="button"
                  aria-label={label}
                  onClick={(event) => {
                    event.stopPropagation();
                    onOpenDay();
                  }}
                  sx={chipSx(undefined, muted)}
                >
                  <Typography component="span" variant="caption" noWrap>
                    {label}
                  </Typography>
                </Box>
              );
            })
          : shown.map((entry) =>
              renderEntry ? (
                <Box key={`${entry.key}:${entry.start}`} onClick={(event) => event.stopPropagation()} sx={{ minWidth: 0, overflow: "hidden" }}>
                  {renderEntry(entry)}
                </Box>
              ) : (
                <EventButton
                  key={`${entry.key}:${entry.start}`}
                  entry={entry}
                  zone={zone}
                  use24Hour={use24Hour}
                  hue={hueOf(entry, categoryHues)}
                  selected={isSelected(selected, entry)}
                  onOpen={() => onOpenEntry(entry)}
                />
              )
            )}
        {summary && summary.moreRows > 0 ? (
          <Typography
            component="button"
            variant="caption"
            onClick={(event) => {
              event.stopPropagation();
              onOpenDay();
            }}
            sx={{ border: 0, bgcolor: "transparent", color: "primary.main", textAlign: "left", cursor: "pointer", p: 0 }}
          >
            +{summary.moreRows} more
          </Typography>
        ) : null}
      </Box>
      {grouped && peekOpen ? (
        <Box
          className="lyo-cal-hover"
          onClick={(event) => event.stopPropagation()}
          sx={{
            display: "flex",
            position: "absolute",
            top: 0,
            left: 0,
            zIndex: 21,
            width: 360,
            height: 360,
            overflow: "hidden",
            flexDirection: "column",
            bgcolor: "background.paper",
            border: 1,
            borderColor: "divider",
            borderRadius: 1,
            boxShadow: 8,
          }}
        >
          <TimeGrid days={[day]} zone={zone} use24Hour={use24Hour} categoryHues={categoryHues} selected={selected} onOpenDay={onOpenDay} onOpenEntry={onOpenEntry} maxHeight={300} />
        </Box>
      ) : null}
    </Paper>
  );
}

function TimeGrid({
  days,
  zone,
  use24Hour,
  categoryHues,
  selected,
  onOpenDay,
  onOpenEntry,
  maxHeight = 576,
}: {
  days: CalendarDay[];
  zone: string;
  use24Hour: boolean;
  categoryHues?: Record<string, number>;
  selected: CalendarEntry | null;
  onOpenDay: (date: string) => void;
  onOpenEntry: (entry: CalendarEntry) => void;
  maxHeight?: number;
}) {
  const columns = `3.25rem repeat(${days.length}, minmax(0, 1fr))`;
  const placed = days.map((day) => placeDayEvents(day.entries, day.date, zone));
  const hasAllDay = placed.some((blocks) => blocks.some((block) => block.allDay));
  return (
    <Box sx={{ minWidth: 0 }}>
      <Box sx={{ display: "grid", gridTemplateColumns: columns }}>
        <Box />
        {days.map((day) => (
          <Button key={day.date} onClick={() => onOpenDay(day.date)} sx={{ justifyContent: "flex-start", color: "text.primary", fontSize: 12 }}>
            {WEEKDAYS[dayOfWeek(day.date)]} {Number(day.date.slice(8))}
          </Button>
        ))}
      </Box>
      {hasAllDay ? (
        <Box sx={{ display: "grid", gridTemplateColumns: columns, borderTop: 1, borderColor: "divider" }}>
          <Typography variant="caption" sx={{ textAlign: "right", pr: 0.5, color: "text.secondary" }}>
            All day
          </Typography>
          {days.map((day, dayIndex) => (
            <Box key={day.date} sx={{ borderLeft: 1, borderColor: "divider", p: 0.25 }}>
              {placed[dayIndex]!
                .filter((block) => block.allDay)
                .map((block) => {
                  const entry = day.entries[block.index];
                  if (!entry) return null;
                  return (
                    <EventButton
                      key={`${entry.key}:${entry.start}`}
                      entry={entry}
                      zone={zone}
                      use24Hour={use24Hour}
                      hue={hueOf(entry, categoryHues)}
                      selected={isSelected(selected, entry)}
                      onOpen={() => onOpenEntry(entry)}
                      hideTime
                    />
                  );
                })}
            </Box>
          ))}
        </Box>
      ) : null}
      <Box sx={{ maxHeight, overflow: "auto", border: 1, borderColor: "divider", borderRadius: 1 }}>
        <Box sx={{ display: "grid", gridTemplateColumns: columns }}>
          <Box>
            {Array.from({ length: 24 }, (_, hour) => (
              <Box key={hour} sx={{ height: 48, borderTop: 1, borderColor: "divider", textAlign: "right", pr: 0.5, fontSize: 11, color: "text.secondary" }}>
                {hourLabel(hour, use24Hour)}
              </Box>
            ))}
          </Box>
          {days.map((day, dayIndex) => (
            <Box key={day.date} onClick={() => onOpenDay(day.date)} sx={{ position: "relative", borderLeft: 1, borderColor: "divider" }}>
              {Array.from({ length: 24 }, (_, hour) => (
                <Box key={hour} sx={{ height: 48, borderTop: 1, borderColor: "divider" }} />
              ))}
              {placed[dayIndex]!
                .filter((block) => !block.allDay)
                .map((block) => {
                  const entry = day.entries[block.index];
                  if (!entry) return null;
                  return <TimedBlock key={`${entry.key}:${entry.start}:${block.index}`} entry={entry} block={block} zone={zone} use24Hour={use24Hour} hue={hueOf(entry, categoryHues)} selected={isSelected(selected, entry)} onOpen={() => onOpenEntry(entry)} />;
                })}
            </Box>
          ))}
        </Box>
      </Box>
    </Box>
  );
}

function TimedBlock({
  entry,
  block,
  zone,
  use24Hour,
  hue,
  selected,
  onOpen,
}: {
  entry: CalendarEntry;
  block: DayBlock;
  zone: string;
  use24Hour: boolean;
  hue?: number;
  selected: boolean;
  onOpen: () => void;
}) {
  const width = 100 / block.laneCount;
  const left = block.lane * width;
  return (
    <Box
      component="button"
      type="button"
      aria-label={`${clockLabel(entry, zone, use24Hour)} ${entry.title}`}
      onClick={(event) => {
        event.stopPropagation();
        onOpen();
      }}
      sx={{
        ...chipSx(hue, entry.muted),
        position: "absolute",
        zIndex: 1,
        top: `${block.topPercent}%`,
        height: `${Math.max(block.heightPercent, 2)}%`,
        left: `calc(${left}% + 1px)`,
        width: `calc(${width}% - 2px)`,
        minHeight: 20,
        alignItems: "flex-start",
        outline: selected ? "2px solid" : "none",
        outlineColor: "primary.main",
      }}
    >
      <Typography component="span" variant="caption" sx={{ flex: "none" }}>
        {clockLabel(entry, zone, use24Hour)}
      </Typography>
      <Typography component="span" variant="caption" sx={{ minWidth: 0, overflow: "hidden" }}>
        {entry.title}
      </Typography>
    </Box>
  );
}

function groupLabel(row: MonthRow, day: CalendarDay, use24Hour: boolean): string {
  const entry = day.entries[row.indexes[0]!];
  if (row.allDay) return row.indexes.length === 1 ? (entry?.title ?? "All day") : `All day · ${row.indexes.length}`;
  return `${hourLabel(row.hour, use24Hour)} · ${row.indexes.length}`;
}

function hourLabel(hour: number, use24Hour: boolean): string {
  if (use24Hour) return `${String(hour).padStart(2, "0")}:00`;
  const suffix = hour < 12 ? "AM" : "PM";
  const clock = hour % 12 || 12;
  return `${clock} ${suffix}`;
}

function isSelected(selected: CalendarEntry | null, entry: CalendarEntry): boolean {
  return selected === entry;
}

function chipSx(hue?: number, muted = false) {
  return {
    display: "flex",
    gap: 0.5,
    width: "100%",
    minWidth: 0,
    overflow: "hidden",
    px: 0.5,
    py: 0.25,
    border: 0,
    borderLeft: "3px solid",
    borderLeftColor: hueColor(hue ?? 207),
    borderRadius: 0.5,
    bgcolor: "action.hover",
    color: "text.primary",
    textAlign: "left",
    cursor: "pointer",
    opacity: muted ? 0.45 : 1,
    filter: muted ? "grayscale(1)" : "none",
  } as const;
}

function EventButton({
  entry,
  zone,
  use24Hour,
  hue,
  selected,
  onOpen,
  hideTime,
}: {
  entry: CalendarEntry;
  zone: string;
  use24Hour: boolean;
  hue?: number;
  selected: boolean;
  onOpen: () => void;
  hideTime?: boolean;
}) {
  const time = clockLabel(entry, zone, use24Hour);
  const tip = entry.description ? `${entry.title}\n${entry.description}` : entry.title;
  return (
    <Tooltip title={<span style={{ whiteSpace: "pre-wrap" }}>{tip}</span>} placement="top" enterDelay={300}>
      <Box
        component="button"
        type="button"
        aria-label={`${hideTime ? "" : `${time} `}${entry.title}`}
        onClick={(event) => {
          event.stopPropagation();
          onOpen();
        }}
        sx={{
          ...chipSx(hue, entry.muted),
          outline: selected ? "2px solid" : "none",
          outlineColor: "primary.main",
        }}
      >
        {hideTime ? null : (
          <Typography component="span" variant="caption" sx={{ flex: "none" }}>
            {time}
          </Typography>
        )}
        <Typography component="span" variant="caption" sx={{ minWidth: 0, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
          {entry.title}
        </Typography>
      </Box>
    </Tooltip>
  );
}

function RefList({ entry, hue, renderRef }: { entry: CalendarEntry; hue?: number; renderRef?: (entity: CalendarEntityRef) => ReactNode }) {
  if (!entry.refs || entry.refs.length === 0) return null;
  return (
    <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 0.5 }}>
      {entry.refs.map((entity) =>
        renderRef ? (
          <Box key={`${entity.entityType}:${entity.entityId}`}>{renderRef(entity)}</Box>
        ) : (
          <Chip key={`${entity.entityType}:${entity.entityId}`} size="small" label={refLabel(entity)} sx={hue == null ? undefined : { bgcolor: hueColor(hue), color: "common.white" }} />
        )
      )}
    </Box>
  );
}

function clockLabel(entry: CalendarEntry, zone: string, use24Hour: boolean): string {
  const start = formatCalendarClock(new Date(entry.start), zone, use24Hour);
  if (entry.start === entry.end) return start;
  return `${start}–${formatCalendarClock(new Date(entry.end), zone, use24Hour)}`;
}

function hueOf(entry: CalendarEntry, categoryHues?: Record<string, number>): number | undefined {
  if (entry.hue != null) return entry.hue;
  if (entry.category && categoryHues && categoryHues[entry.category] != null) return categoryHues[entry.category];
  return undefined;
}

function refLabel(entity: CalendarEntityRef): string {
  const id = entity.entityId.length <= 8 ? entity.entityId : entity.entityId.slice(0, 8);
  return `${entity.entityType} · ${id}`;
}

function visibleDays(calendar: CalendarModel | null, view: ViewMode, focus: string | null): CalendarDay[] {
  const byDate = new Map((calendar?.days ?? []).map((day) => [day.date, day]));
  if (view === "month") return calendar?.days ?? [];
  if (!focus) return [];
  if (view === "day") return [byDate.get(focus) ?? { date: focus, entries: [] }];
  const start = addDays(focus, -dayOfWeek(focus));
  return Array.from({ length: 7 }, (_, index) => {
    const date = addDays(start, index);
    return byDate.get(date) ?? { date, entries: [] };
  });
}

function headerLabels(days: CalendarDay[], view: ViewMode): string[] {
  if (days.length === 0) return view === "day" ? [] : WEEKDAYS;
  if (view === "day") return [WEEKDAYS[dayOfWeek(days[0]!.date)]!];
  const start = dayOfWeek(days[0]!.date);
  return Array.from({ length: 7 }, (_, i) => WEEKDAYS[(start + i) % 7]!);
}

function rangeTitle(days: CalendarDay[], view: ViewMode, calendar: CalendarModel | null): string {
  if (view === "month" || days.length === 0) return monthTitle(calendar);
  if (view === "day") return longDate(days[0]!.date);
  return `${shortDate(days[0]!.date)} – ${shortDate(days[days.length - 1]!.date)}`;
}

function monthTitle(calendar: CalendarModel | null): string {
  if (!calendar || calendar.days.length === 0) return "";
  const focus = calendar.days[Math.floor(calendar.days.length / 2)]!.date;
  const [year, month] = focus.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, 1)).toLocaleString("en-US", { month: "long", year: "numeric", timeZone: "UTC" });
}

function middleDate(calendar: CalendarModel | null): string | null {
  if (!calendar || calendar.days.length === 0) return null;
  return calendar.days[Math.floor(calendar.days.length / 2)]!.date;
}

function shortDate(iso: string): string {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-US", { month: "short", day: "numeric", timeZone: "UTC" });
}

function longDate(iso: string): string {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric", year: "numeric", timeZone: "UTC" });
}

function shiftMonth(iso: string, delta: number): string {
  const [year, month] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(year, month - 1 + delta, 1));
  const y = dt.getUTCFullYear();
  const m = String(dt.getUTCMonth() + 1).padStart(2, "0");
  return `${y}-${m}-01`;
}

function addDays(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(year, month - 1, day + days));
  const y = dt.getUTCFullYear();
  const m = String(dt.getUTCMonth() + 1).padStart(2, "0");
  const d = String(dt.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function dayOfWeek(iso: string): number {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).getUTCDay();
}
