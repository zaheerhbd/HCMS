using HCM.Application.Interfaces;
using NHapi.Base.Parser;
using NHapi.Model.V251.Message;

namespace HCM.Infrastructure.Services;

// Parses HL7 v2.5.1 pipe-delimited messages using NHapi.
// Only ADT^A01 (patient admit) and ORU^R01 (observation result) are supported.
public class Hl7MappingService : IHl7MappingService
{
    private static readonly PipeParser Parser = new();

    public Hl7PatientData ParseAdtA01(string hl7Message)
    {
        var message = Parser.Parse(hl7Message.Replace("\n", "\r"));

        if (message is not ADT_A01 adt)
            throw new InvalidOperationException($"Received '{message.GetType().Name}' but expected ADT_A01. Only HL7 v2.5.1 ADT^A01 is supported.");

        var pid = adt.PID;

        string? firstName = null, lastName = null;
        if (pid.PatientNameRepetitionsUsed > 0)
        {
            var name = pid.GetPatientName(0);
            lastName = name.FamilyName.Surname.Value;
            firstName = name.GivenName.Value;
        }

        DateTime? dob = null;
        var dobStr = pid.DateTimeOfBirth.Time.Value;
        if (!string.IsNullOrEmpty(dobStr) && dobStr.Length >= 8 &&
            DateTime.TryParseExact(dobStr[..8], "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var parsedDob))
            dob = parsedDob;

        var genderCode = pid.AdministrativeSex.Value?.Trim().ToUpperInvariant();
        var gender = genderCode switch
        {
            "M" => "Male",
            "F" => "Female",
            "O" or "A" => "NonBinary",
            _ => null
        };

        string? phone = null;
        if (pid.PhoneNumberHomeRepetitionsUsed > 0)
            phone = pid.GetPhoneNumberHome(0).TelephoneNumber.Value;

        string? externalId = null;
        if (pid.PatientIdentifierListRepetitionsUsed > 0)
            externalId = pid.GetPatientIdentifierList(0).IDNumber.Value;

        return new Hl7PatientData(firstName, lastName, dob, gender, phone, externalId);
    }

    public Hl7ObservationData ParseOruR01(string hl7Message)
    {
        var message = Parser.Parse(hl7Message.Replace("\n", "\r"));

        if (message is not ORU_R01 oru)
            throw new InvalidOperationException($"Received '{message.GetType().Name}' but expected ORU_R01. Only HL7 v2.5.1 ORU^R01 is supported.");

        var patientResult = oru.GetPATIENT_RESULT(0);
        var pid = patientResult.PATIENT.PID;

        string? patientId = null;
        if (pid.PatientIdentifierListRepetitionsUsed > 0)
            patientId = pid.GetPatientIdentifierList(0).IDNumber.Value;

        var orderObs = patientResult.GetORDER_OBSERVATION(0);
        var obx = orderObs.GetOBSERVATION(0).OBX;

        var observationType = obx.ObservationIdentifier.Text.Value;
        var observationValue = obx.GetObservationValue(0).Data?.ToString();
        var units = obx.Units?.Identifier?.Value;

        return new Hl7ObservationData(patientId, observationType, observationValue, units);
    }
}
