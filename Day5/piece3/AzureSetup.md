# Azure Container Apps Setup

## Notes on Region

The target region `centralindia` is blocked by the Azure for Students subscription policy
(`sys.regionrestriction`). The policy allows only: `koreacentral`, `southeastasia`,
`eastasia`, `austriaeast`, `malaysiawest`. Region `southeastasia` was used instead —
it fully supports Azure Container Apps (Microsoft.App provider).

## Commands

### 1. Create Resource Group

```bash
az group create -n thinkschool-rg -l southeastasia
```

### 2. Create Container Apps Environment

```bash
az containerapp env create -n thinkschool-env -g thinkschool-rg -l southeastasia --logs-destination none
```