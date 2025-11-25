using System;
using System.Collections.Generic;
using System.Text;

namespace DetectorVulnerabilitatsDatabase.Models
{
    public class Solutions
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Remediation_steps { get; set; }
        public string References { get; set; }

        public Findings Findings { get; set; }
    }
}
