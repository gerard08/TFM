using System.ComponentModel.DataAnnotations;

namespace Worker.Models
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
        [EnumDataType(typeof(ScanType))] // Assegura que és un valor vàlid de l'enum
        public ScanType ScanType { get; set; } = 0;
    }

    public enum ScanType
    {
        Services = 0,
        WebEnumeration = 1,
        WebVuln = 2,
        CmsScan = 3,
        VulnDb = 4
    }
}
