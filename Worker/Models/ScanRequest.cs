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
        [EnumDataType(typeof(ScanTypeEnum))] // Assegura que és un valor vàlid de l'enum
        public ScanTypeEnum ScanType { get; set; } = 0;

        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    }

    public enum ScanTypeEnum
    {
        Services = 0,
        Infrastructure = 1,
        WebEnumeration = 2,
        WebVuln = 3,
        DDBB = 4
    }
}
