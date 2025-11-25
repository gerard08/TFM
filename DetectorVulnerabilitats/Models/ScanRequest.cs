namespace DetectorVulnerabilitats.Models
{
    public class ScanRequest
    {
        /// <summary>
        /// Target of the Scan
        /// </summary>
        public string Target { get; set; } = null!;

        /// <summary>
        /// Type of the Scan that the user wants to perform
        /// </summary>
        public string ScanType { get; set; } = null!;
    }
}
