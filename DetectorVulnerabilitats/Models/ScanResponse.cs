namespace Worker.Models
{
    public class ScanResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string State { get; set; } = null!;
        public string ScanType { get; set; } = null!;
        public string Target { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string VulnerabilityCount { get; set; } = null!;
        public string Duration { get; set; } = null!;
    }
}
