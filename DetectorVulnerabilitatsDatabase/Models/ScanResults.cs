namespace DetectorVulnerabilitatsDatabase.Models
{
    public class ScanResults
    {
        public Guid Id { get; set; }
        public Guid Scan_task_id { get; set; }
        public string Summary { get; set; }
        public DateTime Created_at { get; set; }

        public ScanTask ScanTask { get; set; }
        public IEnumerable<Findings> Findings { get; set; }
    }
}
