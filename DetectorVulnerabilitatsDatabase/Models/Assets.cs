namespace DetectorVulnerabilitatsDatabase.Models
{
    public class Assets
    {
        public Guid Id { get; set; }
        public string Ip { get; set; }
        public DateTime First_scanned_at { get; set; }
        public DateTime Last_scanned_at { get; set; }

        public ICollection<ScanTask> ScanTasks { get; set; }
    }
}
