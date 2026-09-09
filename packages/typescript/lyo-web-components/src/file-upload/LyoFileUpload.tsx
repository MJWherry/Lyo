"use client";

import { useCallback, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import CloudUpload from "@mui/icons-material/CloudUpload";

export type LyoFileUploadProps = {
  maxFiles?: number;
  maxFileSize?: number;
  accept?: string;
  onFiles?: (files: File[]) => void;
  onUpload?: (file: File) => Promise<void>;
  uploadUrl?: string;
  buttonText?: string;
  chipFileNameMaxLength?: number;
};

function shortName(name: string, max: number): string {
  if (name.length <= max) return name;
  const dot = name.lastIndexOf(".");
  const ext = dot >= 0 ? name.slice(dot) : "";
  const keep = Math.max(1, max - ext.length - 1);
  return `${name.slice(0, keep)}…${ext}`;
}

export function LyoFileUpload({
  maxFiles = 5,
  maxFileSize = 3 * 1024 * 1024 * 1024,
  accept,
  onFiles,
  onUpload,
  uploadUrl,
  buttonText = "Upload",
  chipFileNameMaxLength = 32,
}: LyoFileUploadProps) {
  const [files, setFiles] = useState<File[]>([]);
  const [progress, setProgress] = useState<Record<string, number>>({});
  const [drag, setDrag] = useState(false);

  const add = useCallback(
    (list: FileList | File[]) => {
      const incoming = Array.from(list).filter((f) => f.size <= maxFileSize);
      setFiles((cur) => {
        const next = [...cur, ...incoming].slice(0, maxFiles);
        onFiles?.(next);
        return next;
      });
    },
    [maxFileSize, maxFiles, onFiles]
  );

  const uploadOne = async (file: File) => {
    if (onUpload) {
      setProgress((p) => ({ ...p, [file.name]: 50 }));
      await onUpload(file);
      setProgress((p) => ({ ...p, [file.name]: 100 }));
      return;
    }
    if (!uploadUrl) return;
    const body = new FormData();
    body.append("file", file);
    setProgress((p) => ({ ...p, [file.name]: 30 }));
    await fetch(uploadUrl, { method: "POST", body });
    setProgress((p) => ({ ...p, [file.name]: 100 }));
  };

  return (
    <Stack spacing={1}>
      <Box
        className={drag ? "lyo-file-upload__drop lyo-file-upload__drop--active" : "lyo-file-upload__drop"}
        onDragOver={(e) => {
          e.preventDefault();
          setDrag(true);
        }}
        onDragLeave={() => setDrag(false)}
        onDrop={(e) => {
          e.preventDefault();
          setDrag(false);
          if (e.dataTransfer.files) add(e.dataTransfer.files);
        }}
      >
        <Button component="label" variant="contained" startIcon={<CloudUpload />}>
          {buttonText}
          <input
            hidden
            type="file"
            multiple
            accept={accept}
            onChange={(e) => {
              if (e.target.files) add(e.target.files);
            }}
          />
        </Button>
        <Typography variant="caption" display="block" sx={{ mt: 1 }}>
          Drop files here (max {maxFiles})
        </Typography>
      </Box>
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        {files.map((f) => (
          <Chip
            key={f.name}
            label={shortName(f.name, chipFileNameMaxLength)}
            onDelete={() => setFiles((cur) => cur.filter((x) => x !== f))}
            onClick={() => void uploadOne(f)}
          />
        ))}
      </Stack>
      {Object.values(progress).some((n) => n < 100) ? <LinearProgress /> : null}
    </Stack>
  );
}
