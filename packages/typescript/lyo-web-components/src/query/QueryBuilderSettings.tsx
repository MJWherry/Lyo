"use client";

import FormControlLabel from "@mui/material/FormControlLabel";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import Typography from "@mui/material/Typography";

export type QueryBuilderSettingsProps = {
  allowDragDrop: boolean;
  autoSelectNewFilterNode: boolean;
  selectedLabel?: string;
  onAllowDragDrop: (value: boolean) => void;
  onAutoSelectNewFilterNode: (value: boolean) => void;
};

export function QueryBuilderSettings({
  allowDragDrop,
  autoSelectNewFilterNode,
  selectedLabel = "None",
  onAllowDragDrop,
  onAutoSelectNewFilterNode,
}: QueryBuilderSettingsProps) {
  return (
    <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
      <FormControlLabel
        control={<Switch size="small" checked={allowDragDrop} onChange={(e) => onAllowDragDrop(e.target.checked)} />}
        label="Drag filters"
      />
      <FormControlLabel
        control={
          <Switch
            size="small"
            checked={autoSelectNewFilterNode}
            onChange={(e) => onAutoSelectNewFilterNode(e.target.checked)}
          />
        }
        label="Select new filter"
      />
      <Typography variant="body2" color="text.secondary">
        Selected: {selectedLabel}
      </Typography>
    </Stack>
  );
}
