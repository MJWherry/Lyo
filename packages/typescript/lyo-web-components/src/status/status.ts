import CheckCircle from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import Warning from "@mui/icons-material/Warning";
import Info from "@mui/icons-material/Info";
import Cancel from "@mui/icons-material/Cancel";
import SkipNext from "@mui/icons-material/SkipNext";
import Timer from "@mui/icons-material/Timer";
import Help from "@mui/icons-material/Help";
import type { SvgIconComponent } from "@mui/icons-material";

export type LyoStatusColor = "success" | "error" | "warning" | "info" | "secondary" | "inherit";

export function statusColor(status: string): LyoStatusColor {
  switch (status) {
    case "Success":
      return "success";
    case "Failure":
      return "error";
    case "Success with warnings":
    case "Timed out":
      return "warning";
    case "Partial Success":
      return "info";
    case "Cancelled":
    case "Skipped":
      return "secondary";
    default:
      return "inherit";
  }
}

export function statusIcon(status: string): SvgIconComponent {
  switch (status) {
    case "Success":
      return CheckCircle;
    case "Failure":
      return ErrorIcon;
    case "Success with warnings":
      return Warning;
    case "Partial Success":
      return Info;
    case "Cancelled":
      return Cancel;
    case "Skipped":
      return SkipNext;
    case "Timed out":
      return Timer;
    default:
      return Help;
  }
}
