"use client";

import type { ReactNode } from "react";
import Alert, { type AlertProps } from "@mui/material/Alert";
import AppBar, { type AppBarProps } from "@mui/material/AppBar";
import Autocomplete, { type AutocompleteProps } from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button, { type ButtonProps } from "@mui/material/Button";
import Card, { type CardProps } from "@mui/material/Card";
import Checkbox, { type CheckboxProps } from "@mui/material/Checkbox";
import Chip, { type ChipProps } from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Container, { type ContainerProps } from "@mui/material/Container";
import Divider from "@mui/material/Divider";
import Drawer, { type DrawerProps } from "@mui/material/Drawer";
import FormControl from "@mui/material/FormControl";
import Grid, { type GridProps } from "@mui/material/Grid";
import IconButton, { type IconButtonProps } from "@mui/material/IconButton";
import InputLabel from "@mui/material/InputLabel";
import LinearProgress from "@mui/material/LinearProgress";
import Menu, { type MenuProps } from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Paper, { type PaperProps } from "@mui/material/Paper";
import Select, { type SelectProps } from "@mui/material/Select";
import Skeleton, { type SkeletonProps } from "@mui/material/Skeleton";
import Stack, { type StackProps } from "@mui/material/Stack";
import Switch, { type SwitchProps } from "@mui/material/Switch";
import Tab from "@mui/material/Tab";
import Tabs, { type TabsProps } from "@mui/material/Tabs";
import TextField, { type TextFieldProps } from "@mui/material/TextField";
import Tooltip, { type TooltipProps } from "@mui/material/Tooltip";
import { DateTimePicker, type DateTimePickerProps } from "@mui/x-date-pickers/DateTimePicker";
import type { Dayjs } from "dayjs";
import { resolveElementId } from "../provider/elementId.js";

type IdProps = { elementId?: string; defaultId?: string };

function withId<P extends { id?: string }>(
  Comp: React.ComponentType<P>,
  fallback: string
) {
  return function Wrapped(props: P & IdProps) {
    const { elementId, defaultId, ...rest } = props;
    const id = resolveElementId(elementId, defaultId ?? fallback);
    return <Comp id={id} {...(rest as P)} />;
  };
}

export const LyoButton = withId(Button, "lyo-button") as (
  p: ButtonProps & IdProps
) => ReactNode;
export const LyoIconButton = withId(IconButton, "lyo-icon-button") as (
  p: IconButtonProps & IdProps
) => ReactNode;
export const LyoTextField = withId(TextField, "lyo-text-field") as (
  p: TextFieldProps & IdProps
) => ReactNode;

export function LyoNumericField({
  value,
  onChange,
  label,
  elementId,
  min,
  max,
  ...rest
}: {
  value: number;
  onChange: (n: number) => void;
  label?: string;
  min?: number;
  max?: number;
} & Omit<TextFieldProps, "value" | "onChange" | "type"> &
  IdProps) {
  return (
    <TextField
      id={resolveElementId(elementId, "lyo-numeric")}
      label={label}
      type="number"
      value={Number.isFinite(value) ? value : ""}
      onChange={(e) => {
        const n = Number(e.target.value);
        if (!Number.isFinite(n)) return;
        const clamped = Math.min(max ?? n, Math.max(min ?? n, n));
        onChange(clamped);
      }}
      {...rest}
    />
  );
}

export const LyoSwitch = withId(Switch, "lyo-switch") as (p: SwitchProps & IdProps) => ReactNode;
export const LyoCheckbox = withId(Checkbox, "lyo-checkbox") as (p: CheckboxProps & IdProps) => ReactNode;
export const LyoSelect = Select;
export const LyoAutocomplete = Autocomplete;
export const LyoChip = Chip;
export const LyoAlert = Alert;
export const LyoTooltip = Tooltip;
export const LyoMenu = Menu;
export const LyoMenuItem = MenuItem;
export const LyoTabs = Tabs;
export const LyoTab = Tab;
export const LyoDivider = Divider;
export const LyoStack = Stack;
export const LyoGrid = Grid;
export const LyoCard = Card;
export const LyoPaper = Paper;
export const LyoContainer = Container;
export const LyoAppBar = AppBar;
export const LyoDrawer = Drawer;
export const LyoSkeleton = Skeleton;
export const LyoFormControl = FormControl;
export const LyoInputLabel = InputLabel;

export function LyoSpacer() {
  return <Box sx={{ flexGrow: 1 }} />;
}

export function LyoProgress({ variant = "circular", size }: { variant?: "circular" | "linear"; size?: number }) {
  return variant === "linear" ? <LinearProgress /> : <CircularProgress size={size} />;
}

export function LyoDatePicker(props: DateTimePickerProps & IdProps) {
  const { elementId, defaultId, ...rest } = props;
  return (
    <DateTimePicker
      slotProps={{
        textField: { id: resolveElementId(elementId, defaultId ?? "lyo-date") },
      }}
      {...rest}
    />
  );
}

export type {
  AlertProps,
  AppBarProps,
  AutocompleteProps,
  ButtonProps,
  CardProps,
  CheckboxProps,
  ChipProps,
  ContainerProps,
  DrawerProps,
  GridProps,
  IconButtonProps,
  MenuProps,
  PaperProps,
  SelectProps,
  SkeletonProps,
  StackProps,
  SwitchProps,
  TabsProps,
  TextFieldProps,
  TooltipProps,
  Dayjs,
};
