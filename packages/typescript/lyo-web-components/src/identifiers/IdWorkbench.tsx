"use client";

import { useState } from "react";
import { nanoid } from "nanoid";
import { ulid } from "ulid";
import { v3 as uuidv3, v4 as uuidv4, v5 as uuidv5, v6 as uuidv6, v7 as uuidv7 } from "uuid";
import KSUID from "ksuid";
import Button from "@mui/material/Button";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Typography from "@mui/material/Typography";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";

const DNS = "6ba7b810-9dad-11d1-80b4-00c04fd430c8";
const URL_NS = "6ba7b811-9dad-11d1-80b4-00c04fd430c8";

function snowflake(workerId = 1): string {
  const ts = BigInt(Date.now()) - 1288834974657n;
  const id = (ts << 22n) | (BigInt(workerId & 0x1f) << 12n) | BigInt(Math.floor(Math.random() * 4096));
  return id.toString();
}

export function IdResultPanel({ entries }: { entries: string[] }) {
  const snack = useLyoSnackbar();
  return (
    <Stack spacing={0.5}>
      {entries.map((e) => (
        <Paper
          key={e}
          variant="outlined"
          sx={{ p: 1, fontFamily: "monospace", fontSize: 13, cursor: "pointer" }}
          onClick={() => {
            void navigator.clipboard.writeText(e);
            snack.show("Copied", "success");
          }}
        >
          {e}
        </Paper>
      ))}
    </Stack>
  );
}

export function IdWorkbench() {
  const [tab, setTab] = useState(0);
  const [count, setCount] = useState(5);
  const [guidVer, setGuidVer] = useState<"v4" | "v7" | "v6" | "v3" | "v5">("v7");
  const [name, setName] = useState("example.com");
  const [entries, setEntries] = useState<string[]>([]);

  const generate = async () => {
    const n = Math.min(100, Math.max(1, count));
    const next: string[] = [];
    if (tab === 0) {
      for (let i = 0; i < n; i++) {
        if (guidVer === "v4") next.push(uuidv4());
        else if (guidVer === "v7") next.push(uuidv7());
        else if (guidVer === "v6") next.push(uuidv6());
        else if (guidVer === "v3") next.push(uuidv3(name, DNS));
        else next.push(uuidv5(name, URL_NS));
      }
    } else if (tab === 1) {
      for (let i = 0; i < n; i++) next.push((await KSUID.random()).string);
    } else if (tab === 2) {
      for (let i = 0; i < n; i++) next.push(ulid());
    } else if (tab === 3) {
      for (let i = 0; i < n; i++) next.push(nanoid());
    } else {
      for (let i = 0; i < n; i++) next.push(snowflake());
    }
    setEntries(next);
  };

  return (
    <Paper sx={{ p: 2 }}>
      <Typography variant="h5">ID Generator</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Generate GUID, KSUID, ULID, NanoID, and Snowflake identifiers.
      </Typography>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="UUID / GUID" />
        <Tab label="KSUID" />
        <Tab label="ULID" />
        <Tab label="NanoID" />
        <Tab label="Snowflake" />
      </Tabs>
      <Stack direction="row" spacing={1} sx={{ mb: 2 }} alignItems="center">
        {tab === 0 ? (
          <TextField
            size="small"
            select
            label="Version"
            value={guidVer}
            onChange={(e) => setGuidVer(e.target.value as typeof guidVer)}
          >
            <MenuItem value="v4">V4 — Random</MenuItem>
            <MenuItem value="v7">V7 — Unix-ms ordered</MenuItem>
            <MenuItem value="v6">V6 — Gregorian ordered</MenuItem>
            <MenuItem value="v3">V3 — MD5 name-based</MenuItem>
            <MenuItem value="v5">V5 — SHA-1 name-based</MenuItem>
          </TextField>
        ) : null}
        {tab === 0 && (guidVer === "v3" || guidVer === "v5") ? (
          <TextField size="small" label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        ) : (
          <TextField
            size="small"
            type="number"
            label="Count"
            value={count}
            onChange={(e) => setCount(Number(e.target.value) || 1)}
          />
        )}
        <Button variant="contained" onClick={() => void generate()}>
          Generate
        </Button>
      </Stack>
      <IdResultPanel entries={entries} />
    </Paper>
  );
}
