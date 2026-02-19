# .NET Security Report for {{date}}

The following vulnerabilities were disclosed this month.

| CVE | Severity | Component |
| --- | -------- | --------- |
| {{cve1}} | {{severity1}} | {{component1}} |
| {{cve2}} | {{severity2}} | {{component2}} |
| {{cve3}} | {{severity3}} | {{component3}} |

## Affected Products

| Product | Version | Status |
| ------- | ------- | ------ |
| {{product1}} | {{version1}} | {{status1}} |
| {{product2}} | {{version2}} | {{status2}} |

## Severity Definitions

| Level | CVSS Range | Response |
| ----- | ---------- | -------- |
| Critical | 9.0–10.0 | Patch immediately |
| High | 7.0–8.9 | Patch within 30 days |
| Medium | 4.0–6.9 | Patch at next cycle |
| Low | 0.1–3.9 | Risk-accepted |

{{#if commits}}

## Related Commits

The following commits address the vulnerabilities above.

| SHA | Message | Author |
| --- | ------- | ------ |
| {{sha1}} | {{message1}} | {{author1}} |
| {{sha2}} | {{message2}} | {{author2}} |
{{/if}}
