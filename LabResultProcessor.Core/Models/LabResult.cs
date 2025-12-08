using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabResultProcessor.Core.Models
{
    public class LabResult
    {
        public string PatientId { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string OrderId { get; set; } = default!;
        public string TestCode { get; set; } = default!;
        public string TestDescription { get; set; } = default!;
        public string ResultValue { get; set; } = default!;
        public string Units { get; set; } = default!;
        public string ReferenceRange { get; set; } = default!;
        public string AbnormalFlag { get; set; } = default!;
        public DateTime ObservationDateTime { get; set; }
        public string ResultStatus { get; set; } = default!;

        // DynamoDB keys
        public string PatientPk => PatientId;
        public string ResultKey => $"{OrderId}#{TestCode}#{ObservationDateTime:yyyyMMddHHmmss}";
    }
}
