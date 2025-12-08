using LabResultProcessor.Core.Models;
using NHapi.Base.Parser;
using NHapi.Base.Model;
using ORU_R01_251 = NHapi.Model.V251.Message.ORU_R01;

namespace LabResultProcessor.Core.Parsing;

public class Hl7LabResultParser
{
    private readonly PipeParser _parser = new();

    public IList<LabResult> ParseLabResults(string hl7Message)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
            throw new ArgumentException("HL7 message is empty", nameof(hl7Message));

        IMessage message = _parser.Parse(hl7Message);

        if (message is not ORU_R01_251 oru)
            throw new InvalidOperationException($"Unsupported HL7 message type: {message.GetType().Name}");

        var results = new List<LabResult>();

        // ORU_R01 has PATIENT_RESULT group(s) -> PATIENT + ORDER_OBSERVATION -> OBX segments
        for (int i = 0; i < oru.PATIENT_RESULTRepetitionsUsed; i++)
        {
            var patientResult = oru.GetPATIENT_RESULT(i);
            var patient = patientResult.PATIENT;
            var pid = patient.PID;

            string patientId = pid.GetPatientIdentifierList(0).IDNumber.Value ?? string.Empty;
            string lastName = pid.GetPatientName(0).FamilyName?.Surname?.Value ?? string.Empty;
            string firstName = pid.GetPatientName(0).GivenName?.Value ?? string.Empty;

            for (int j = 0; j < patientResult.ORDER_OBSERVATIONRepetitionsUsed; j++)
            {
                var orderObs = patientResult.GetORDER_OBSERVATION(j);
                var obr = orderObs.OBR;
                string orderId = obr.FillerOrderNumber?.EntityIdentifier?.Value
                                 ?? obr.PlacerOrderNumber?.EntityIdentifier?.Value
                                 ?? string.Empty;

                for (int k = 0; k < orderObs.OBSERVATIONRepetitionsUsed; k++)
                {
                    var obx = orderObs.GetOBSERVATION(k);
                    var obxSeg = obx.OBX;

                    string testCode = obxSeg.ObservationIdentifier?.Identifier?.Value ?? string.Empty;
                    string testDesc = obxSeg.ObservationIdentifier?.Text?.Value ?? string.Empty;
                    string value = obxSeg.GetObservationValue().Length > 0
                        ? obxSeg.GetObservationValue()[0].Data.ToString()
                        : string.Empty;
                    string units = obxSeg.Units?.Text?.Value ?? string.Empty;
                    string refRange = obxSeg.ReferencesRange?.Value ?? string.Empty;
                    string abnormal = obxSeg.GetAbnormalFlags().Length > 0
                        ? obxSeg.GetAbnormalFlags()[0].Value
                        : string.Empty;
                    string status = obxSeg.ObservationResultStatus?.Value ?? string.Empty;

                    DateTime obsDateTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(obxSeg.DateTimeOfTheObservation?.Time?.Value) &&
                        DateTime.TryParseExact(
                            obxSeg.DateTimeOfTheObservation.Time.Value,
                            new[] { "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd" },
                            null,
                            System.Globalization.DateTimeStyles.AssumeUniversal,
                            out var parsedDt))
                    {
                        obsDateTime = parsedDt;
                    }

                    results.Add(new LabResult
                    {
                        PatientId = patientId,
                        LastName = lastName,
                        FirstName = firstName,
                        OrderId = orderId,
                        TestCode = testCode,
                        TestDescription = testDesc,
                        ResultValue = value,
                        Units = units,
                        ReferenceRange = refRange,
                        AbnormalFlag = abnormal,
                        ObservationDateTime = obsDateTime,
                        ResultStatus = status
                    });
                }
            }
        }

        return results;
    }
}
