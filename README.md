# Azure Billing Cost Optimization (Serverless + Parquet + ADLS)

## Overview

This solution archives read-heavy billing data from Cosmos DB into Parquet files in Azure Data Lake Storage to reduce cost while maintaining data availability and API compatibility.

## Components
- Cosmos DB (Hot)
- ADLS Gen2 (Cold Storage)
- Azure Functions (Archival + Proxy)
- Parquet Format (Optimized Storage)
- Optional: Azure Synapse Serverless SQL

## Features
- No data loss, no downtime
- Existing API remains unchanged
- Optimized cold storage with Parquet

## How to Deploy
1. Configure environment variables in `local.settings.json`
2. Deploy functions using `func azure functionapp publish`
3. Assign required roles using `infra/roles-setup.sh`

## Architecture

![architecture](docs/architecture.png)