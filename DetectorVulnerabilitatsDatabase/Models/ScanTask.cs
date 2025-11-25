namespace DetectorVulnerabilitatsDatabase.Models
{
    public class ScanTask
    {
        public Guid Id { get; set; }
        public Guid Asset_id { get; set; }
        public string Scan_type { get; set; }
        public string Status { get; set; }
        public string Requested_by { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Started_at { get; set; }
        public DateTime Finished_at { get; set; }

        public Assets Asset { get; set; }
        public ScanResults? ScanResults { get; set; }
    }
}
