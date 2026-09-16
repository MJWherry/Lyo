"use client";

import { useState } from "react";
import { decodeTime, ulid } from "ulid";
import KSUID from "ksuid";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";
import {
  NANO_DEFAULT_ALPHABET,
  NANO_DEFAULT_SIZE,
  autoIncrementIds,
  generateGuids,
  generateNanoIds,
  type AutoIncrementType,
  type GuidNamespaceKind,
  type GuidVersion,
  type IdEntry,
} from "./idOptions.js";

const SNOWFLAKE_EPOCH = 1288834974657n;

const utcNote = (ms: number) => `≈ ${new Date(ms).toISOString().replace("T", " ").replace("Z", "")} UTC`;

function snowflake(machineId: number): IdEntry {
  const ts = BigInt(Date.now()) - SNOWFLAKE_EPOCH;
  const id = (ts << 22n) | (BigInt(machineId & 0x3ff) << 12n) | BigInt(Math.floor(Math.random() * 4096));
  return { value: id.toString(), note: utcNote(Number((id >> 22n) + SNOWFLAKE_EPOCH)) };
}

/** Click-to-copy list of generated ids; notes (embedded time, counter) show under each id. */
export function IdResultPanel({ entries }: { entries: readonly (string | IdEntry)[] }) {
  const snack = useLyoSnackbar();
  return (
    <Stack spacing={0.5}>
      {entries.map((raw) => {
        const entry = typeof raw === "string" ? { value: raw } : raw;
        return (
          <Paper
            key={entry.value}
            variant="outlined"
            sx={{ p: 1, cursor: "pointer" }}
            onClick={() => {
              void navigator.clipboard.writeText(entry.value);
              snack.show("Copied", "success");
            }}
          >
            <Typography sx={{ fontFamily: "monospace", fontSize: 13 }}>{entry.value}</Typography>
            {entry.note ? (
              <Typography variant="caption" color="text.secondary">
                {entry.note}
              </Typography>
            ) : null}
          </Paper>
        );
      })}
    </Stack>
  );
}

const TABS = ["UUID / GUID", "KSUID", "ULID", "NanoID", "Snowflake", "Auto increment"] as const;

/** Generates GUIDs (v3–v7, namespaces), KSUIDs, ULIDs, NanoIDs (alphabet / size), Snowflakes (machine id), and auto-increment runs. */
export function IdWorkbench() {
  const snack = useLyoSnackbar();
  const [tab, setTab] = useState(0);
  const [count, setCount] = useState(5);
  const [guidVersion, setGuidVersion] = useState<GuidVersion>("v7");
  const [namespace, setNamespace] = useState<GuidNamespaceKind>("DNS");
  const [customNamespace, setCustomNamespace] = useState("");
  const [name, setName] = useState("example.com");
  const [alphabet, setAlphabet] = useState(NANO_DEFAULT_ALPHABET);
  const [size, setSize] = useState(NANO_DEFAULT_SIZE);
  const [machineId, setMachineId] = useState(0);
  const [autoType, setAutoType] = useState<AutoIncrementType>("int");
  const [autoStart, setAutoStart] = useState("0");
  const [entries, setEntries] = useState<IdEntry[]>([]);
  const named = tab === 0 && (guidVersion === "v3" || guidVersion === "v5");

  const generate = async () => {
    const n = Math.min(100, Math.max(1, count));
    try {
      if (tab === 0) setEntries(generateGuids(guidVersion, n, { namespace, customNamespace, name }));
      else if (tab === 1)
        setEntries(
          await Promise.all(
            Array.from({ length: n }, async () => {
              const k = await KSUID.random();
              return { value: k.string, note: utcNote(k.date.getTime()) };
            })
          )
        );
      else if (tab === 2)
        setEntries(
          Array.from({ length: n }, () => {
            const value = ulid();
            return { value, note: utcNote(decodeTime(value)) };
          })
        );
      else if (tab === 3) setEntries(generateNanoIds(n, alphabet, size));
      else if (tab === 4) setEntries(Array.from({ length: n }, () => snowflake(machineId)));
      else setEntries(autoIncrementIds(autoType, BigInt(autoStart.trim() || "0"), n));
    } catch (err) {
      snack.show(`Error: ${err instanceof Error ? err.message : String(err)}`, "error");
    }
  };

  return (
    <Paper sx={{ p: 2 }}>
      <Typography variant="h5">ID Generator</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Generate GUID, KSUID, ULID, NanoID, Snowflake, and auto-increment identifiers.
      </Typography>
      <Tabs
        value={tab}
        onChange={(_, v) => {
          setTab(v);
          setEntries([]);
        }}
        variant="scrollable"
        sx={{ mb: 2 }}
      >
        {TABS.map((t) => (
          <Tab key={t} label={t} />
        ))}
      </Tabs>
      <Stack direction="row" spacing={1} sx={{ mb: 2 }} alignItems="center" flexWrap="wrap" useFlexGap>
        {tab === 0 ? (
          <TextField size="small" select label="Version" value={guidVersion} onChange={(e) => setGuidVersion(e.target.value as GuidVersion)}>
            <MenuItem value="v4">V4 — Random</MenuItem>
            <MenuItem value="v7">V7 — Unix-ms ordered</MenuItem>
            <MenuItem value="v6">V6 — Gregorian ordered</MenuItem>
            <MenuItem value="v3">V3 — MD5 name-based</MenuItem>
            <MenuItem value="v5">V5 — SHA-1 name-based</MenuItem>
          </TextField>
        ) : null}
        {named ? (
          <>
            <TextField size="small" select label="Namespace" value={namespace} onChange={(e) => setNamespace(e.target.value as GuidNamespaceKind)} sx={{ width: 130 }}>
              {(["DNS", "URL", "OID", "X500", "Custom"] as const).map((ns) => (
                <MenuItem key={ns} value={ns}>
                  {ns}
                </MenuItem>
              ))}
            </TextField>
            {namespace === "Custom" ? (
              <TextField size="small" label="Namespace GUID" value={customNamespace} onChange={(e) => setCustomNamespace(e.target.value)} sx={{ width: 320 }} />
            ) : null}
            <TextField size="small" label="Name" value={name} onChange={(e) => setName(e.target.value)} />
          </>
        ) : (
          <TextField size="small" type="number" label="Count" value={count} onChange={(e) => setCount(Number(e.target.value) || 1)} sx={{ width: 100 }} />
        )}
        {tab === 3 ? (
          <>
            <TextField size="small" label="Alphabet" value={alphabet} onChange={(e) => setAlphabet(e.target.value)} sx={{ width: 320 }} />
            <TextField size="small" type="number" label="Size" value={size} onChange={(e) => setSize(Number(e.target.value) || 0)} sx={{ width: 90 }} />
          </>
        ) : null}
        {tab === 4 ? (
          <TextField
            size="small"
            type="number"
            label="Machine id"
            value={machineId}
            onChange={(e) => setMachineId(Math.min(1023, Math.max(0, Number(e.target.value) || 0)))}
            helperText="0–1023"
            sx={{ width: 120 }}
          />
        ) : null}
        {tab === 5 ? (
          <>
            <TextField size="small" select label="Type" value={autoType} onChange={(e) => setAutoType(e.target.value as AutoIncrementType)} sx={{ width: 100 }}>
              {(["int", "long", "uint", "ulong"] as const).map((t) => (
                <MenuItem key={t} value={t}>
                  {t}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              label="Start"
              value={autoStart}
              onChange={(e) => setAutoStart(e.target.value.replace(/[^\d-]/g, ""))}
              helperText="First id is start + 1"
              sx={{ width: 200 }}
            />
          </>
        ) : null}
        <Button variant="contained" onClick={() => void generate()}>
          Generate
        </Button>
      </Stack>
      <IdResultPanel entries={entries} />
    </Paper>
  );
}
