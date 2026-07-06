# PHI Audit & SAS URL Verification

HIPAA rule: log entity IDs only — never names, DOB, SSN, MRN strings, insurance numbers.

## PHI Leak Prevention Checklist

| Check | Where to look | Action if found |
|---|---|---|
| Patient name in Serilog logs | Grep codebase for `_logger.Log` with `patient.Name`, `firstName` | Replace with patient ID only |
| DOB / SSN in exception messages | Check ValidationException messages in DataHandlers | Generalise error message |
| MRN in Application Insights traces | Live stream in Azure portal during create-patient E2E flow | Remove from log template |
| SAS URL in any log statement | Grep for `Log.*sasUrl`, `Log.*SasUrl` | Log only blob name, never the URL |
| SAS URL expiry > 1 hour | Capture SAS URL from API response, parse `se=` param | Fix `IBlobStorageService.GetSasUrl` to use `TimeSpan.FromHours(1)` |

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Grep codebase for PHI in log statements — fix any found | Pattern: `_logger.Log.*patient\.Name|_logger.Log.*\.Ssn|_logger.Log.*\.Dob` — run across `src/HCM.Application` + `src/HCM.Infrastructure` | Medium |
| Confirm SAS URLs are not logged — live check in App Insights during download | Use Azure portal Live Metrics stream; download a document; verify no trace contains a URL with `?sv=` or `?sig=` | Low |
