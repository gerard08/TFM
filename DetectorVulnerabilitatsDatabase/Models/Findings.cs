namespace DetectorVulnerabilitatsDatabase.Models
{
    public class Findings
    {
        public Guid Id { get; set; }
        public Guid Scan_result_id { get; set; }
        public string Title { get; set; }
        public string Severity { get; set; }
        public string Cve_id { get; set; }
        public string Affected_service { get; set; }
        public string Description { get; set; }
        public string Solution { get; set; }
        public DateTime Created_at { get; set; }

        public ScanResults ScanResults { get; set; }
    }
}
