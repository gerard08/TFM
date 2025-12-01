import { Findings } from "./findings";

export interface ScanResults {
  id: string;
  scan_task_id: string;
  summary: string;
  created_at: string;
  findings: Findings[];
}
