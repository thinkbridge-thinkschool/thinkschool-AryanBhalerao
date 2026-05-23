# az containerapp env show — Output

Command run:
```
az containerapp env show -n thinkschool-env -g thinkschool-rg
```

```json
{
  "id": "/subscriptions/f2ab3e93-bb60-46ed-bb28-c8c15a1af0f7/resourceGroups/thinkschool-rg/providers/Microsoft.App/managedEnvironments/thinkschool-env",
  "location": "Southeast Asia",
  "name": "thinkschool-env",
  "properties": {
    "appInsightsConfiguration": null,
    "appLogsConfiguration": {
      "destination": null,
      "logAnalyticsConfiguration": null
    },
    "availabilityZones": null,
    "customDomainConfiguration": {
      "certificateKeyVaultProperties": null,
      "certificatePassword": null,
      "certificateValue": null,
      "customDomainVerificationId": "D6690236CDCE4676D905FEB283A69A50641DA7764EB5ABAECB9F39B33CB6923F",
      "dnsSuffix": null,
      "expirationDate": null,
      "subjectName": null,
      "thumbprint": null
    },
    "daprAIConnectionString": null,
    "daprAIInstrumentationKey": null,
    "daprConfiguration": {
      "version": "1.16.4-msft.6"
    },
    "defaultDomain": "blackcliff-61f10661.southeastasia.azurecontainerapps.io",
    "diskEncryptionConfiguration": null,
    "environmentMode": "WorkloadProfiles",
    "eventStreamEndpoint": "https://southeastasia.azurecontainerapps.dev/subscriptions/f2ab3e93-bb60-46ed-bb28-c8c15a1af0f7/resourceGroups/thinkschool-rg/managedEnvironments/thinkschool-env/eventstream",
    "infrastructureResourceGroup": null,
    "ingressConfiguration": null,
    "kedaConfiguration": {
      "version": "2.18.1"
    },
    "openTelemetryConfiguration": null,
    "peerAuthentication": {
      "mtls": {
        "enabled": false
      }
    },
    "peerTrafficConfiguration": {
      "encryption": {
        "enabled": false
      }
    },
    "provisioningState": "Succeeded",
    "publicNetworkAccess": "Enabled",
    "staticIp": "20.198.203.165",
    "vnetConfiguration": null,
    "workloadProfiles": [
      {
        "enableFips": false,
        "name": "Consumption",
        "workloadProfileType": "Consumption"
      }
    ],
    "zoneRedundant": false
  },
  "resourceGroup": "thinkschool-rg",
  "systemData": {
    "createdAt": "2026-05-23T08:43:06.4381974",
    "createdBy": "202101040075@msteams.mitaoe.ac.in",
    "createdByType": "User",
    "lastModifiedAt": "2026-05-23T08:43:06.4381974",
    "lastModifiedBy": "202101040075@msteams.mitaoe.ac.in",
    "lastModifiedByType": "User"
  },
  "type": "Microsoft.App/managedEnvironments"
}
```

### Key fields

| Field | Value |
|---|---|
| `provisioningState` | `Succeeded` | 
| `defaultDomain` | `blackcliff-61f10661.southeastasia.azurecontainerapps.io` |
| `staticIp` | `20.198.203.165` |
| `environmentMode` | `WorkloadProfiles` |
| `kedaConfiguration.version` | `2.18.1` | 
| `daprConfiguration.version` | `1.16.4-msft.6` | 
| `peerAuthentication.mtls.enabled` | `false` |