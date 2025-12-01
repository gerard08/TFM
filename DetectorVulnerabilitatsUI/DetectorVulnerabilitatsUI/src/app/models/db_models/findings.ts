export interface Findings {
  id: string;
  scan_result_id: string;
  title: string;
  severity: string;
  cve_id: string;
  affected_service: string;
  description: string;
  solution: string;
  created_at: string;
}
