# PortaleFatture.BE.ImportFinancepagoPA

Python-based data import suite for loading PagoPA financial data, contracts, and KPMG reports from Parquet files into SQL Server.

## Overview

This Python project provides scripts for importing various types of financial data from Parquet files into SQL Server database. It handles contracts, KPMG reports, financial reports, and schema verification for the PagoPA financial data pipeline.

## Technology Stack

- **Python 3.x**: Core language
- **pandas**: Data manipulation and analysis
- **pyodbc**: SQL Server connectivity
- **azure-identity**: Azure AD authentication
- **dotenv**: Environment variable management
- **logging**: Error tracking and debugging

## Features

- Parquet file processing
- Azure AD authentication to SQL Server
- Multiple data import scripts for different data types
- Environment-specific configuration (dev/uat/prod)
- Comprehensive logging
- Error handling and validation
- Schema checking utilities

## Project Structure

```
PortaleFatture.BE.ImportFinancepagoPA/
├── data/                          # Data files directory
├── delta_table/                   # Delta table storage
├── hadoop_data/                   # Hadoop configuration
├── libs/                          # External libraries
├── Scripts/                       # Python utility scripts
├── Spark/                         # Spark job configurations
├── import_contracts.py            # Contract data import
├── import_kpmg.py                 # KPMG report import
├── import_financial_report.py     # Financial report import
├── import_check_schema.py         # Schema validation
├── .env.dev                       # Dev environment config
├── .env.uat                       # UAT environment config
├── .env.prod                      # Prod environment config
├── core-site.xml                  # Hadoop core config
├── hdfs-site.xml                  # HDFS config
├── mapred-site.xml                # MapReduce config
├── yarn-site.xml                  # YARN config
├── PortaleFatture.BE.ImportFinancepagoPA.pyproj  # Project file
└── README.md                      # This file
```

## Installation

### 1. Install Required Libraries

Ensure you have Python installed, then install dependencies:

```bash
pip install pandas pyodbc azure-identity python-dotenv logging
```

### 2. ODBC Driver

Install the appropriate ODBC driver for SQL Server:
- **Windows**: Download from Microsoft
- **Linux**: Install msodbcsql17 or msodbcsql18
- **macOS**: Install via Homebrew

### 3. Azure Authentication

Configure Azure Active Directory authentication. The scripts use `azure-identity` for AAD authentication to SQL Server.

## Configuration

### Environment Variables

Create `.env` files for each environment (dev/uat/prod):

#### .env.dev
```env
SERVER=your-dev-server.database.windows.net
DATABASE=your-dev-database
TABLE_CONTRACTS=schema.ContractsTable
TABLE_KPMG=schema.KPMGTable
TABLE_FINANCIAL_REPORTS=schema.FinancialReportsTable
```

#### .env.uat
```env
SERVER=your-uat-server.database.windows.net
DATABASE=your-uat-database
TABLE_CONTRACTS=schema.ContractsTable
TABLE_KPMG=schema.KPMGTable
TABLE_FINANCIAL_REPORTS=schema.FinancialReportsTable
```

#### .env.prod
```env
SERVER=your-prod-server.database.windows.net
DATABASE=your-prod-database
TABLE_CONTRACTS=schema.ContractsTable
TABLE_KPMG=schema.KPMGTable
TABLE_FINANCIAL_REPORTS=schema.FinancialReportsTable
```

## Import Scripts

### 1. import_contracts.py

Imports contract data from Parquet files.

**Usage**:
```bash
python import_contracts.py <environment> <parquet_file_path>
```

**Example**:
```bash
python import_contracts.py prod ./data/contracts_2025.parquet
```

**Parameters**:
- `environment`: dev, uat, or prod
- `parquet_file_path`: Path to Parquet file

### 2. import_kpmg.py

Imports KPMG report data from Parquet files.

**Usage**:
```bash
python import_kpmg.py <environment> <parquet_file_path>
```

**Example**:
```bash
python import_kpmg.py prod ./data/kpmg_file.parquet
```

### 3. import_financial_report.py

Imports financial report data from Parquet files.

**Usage**:
```bash
python import_financial_report.py <environment> <parquet_file_path>
```

**Example**:
```bash
python import_financial_report.py prod ./data/financial_report.parquet
```

### 4. import_check_schema.py

Validates database schema and table structure.

**Usage**:
```bash
python import_check_schema.py <environment>
```

**Example**:
```bash
python import_check_schema.py dev
```

## Data Processing

### Parquet Files

The scripts process Parquet files which are:
- Columnar storage format
- Highly compressed
- Schema-preserved
- Efficient for large datasets

### Data Flow

1. **Load**: Read Parquet file using pandas
2. **Transform**: Process and validate data
3. **Connect**: Authenticate to SQL Server via Azure AD
4. **Load**: Insert data into target tables
5. **Log**: Record success/failure

## Authentication

### Azure AD Authentication

The scripts use Azure Active Directory for SQL Server authentication:
- Managed Identity when running in Azure
- Interactive login for local development
- Service Principal for automated jobs

### Connection String Format

```python
connection_string = (
    f"Driver={{ODBC Driver 17 for SQL Server}};"
    f"Server={server};"
    f"Database={database};"
    f"Authentication=ActiveDirectoryInteractive;"
)
```

## Logging

All scripts implement comprehensive logging:
- **INFO**: Process progress and status
- **WARNING**: Non-critical issues
- **ERROR**: Failures and exceptions
- **DEBUG**: Detailed diagnostic information

Log format:
```
[TIMESTAMP] [LEVEL] [SCRIPT] - Message
```

## Error Handling

The scripts include error handling for:
- Missing environment files
- Invalid Parquet files
- Database connection failures
- SQL execution errors
- Data validation errors
- Authentication failures

## Best Practices

1. **Environment Separation**: Always specify the correct environment
2. **Azure Authentication**: Use managed identities in production
3. **Logging**: Review logs for errors and warnings
4. **Validation**: Run schema checks before imports
5. **Backups**: Ensure database backups before large imports
6. **Testing**: Test with dev environment first
7. **Monitoring**: Monitor import performance and errors

## Hadoop Configuration

The project includes Hadoop configuration files for distributed processing:

- **core-site.xml**: Core Hadoop settings
- **hdfs-site.xml**: HDFS configuration
- **mapred-site.xml**: MapReduce settings
- **yarn-site.xml**: YARN resource management

These files support:
- Distributed file system access
- Cluster-based processing
- Resource management
- Data locality

## Spark Integration

The `Spark/` directory contains configurations for:
- PySpark job definitions
- Cluster processing
- Distributed data import
- Large-scale transformations

## Data Tables

### Contracts Table
Stores contract information:
- Contract IDs
- Institution data
- Contract terms
- Effective dates
- Status information

### KPMG Table
Stores KPMG audit and reporting data:
- Audit reports
- Financial analysis
- Compliance data
- Period information

### Financial Reports Table
Stores financial reporting data:
- Transaction details
- Financial metrics
- Period aggregations
- Payment information

## Performance Optimization

### For Large Files

1. **Chunking**: Process Parquet files in chunks
2. **Batch Inserts**: Use bulk insert operations
3. **Parallel Processing**: Leverage Spark for distributed processing
4. **Indexing**: Ensure proper database indexes
5. **Connection Pooling**: Reuse database connections

### Example Chunked Processing

```python
import pandas as pd

# Read Parquet in chunks
chunk_size = 10000
parquet_file = pd.read_parquet('large_file.parquet')

for i in range(0, len(parquet_file), chunk_size):
    chunk = parquet_file[i:i+chunk_size]
    # Process and insert chunk
```

## Troubleshooting

### Common Issues

1. **ODBC Driver Not Found**
   - Install ODBC Driver 17 or 18 for SQL Server
   - Verify installation with `odbcinst -j`

2. **Authentication Failed**
   - Ensure Azure AD credentials are valid
   - Check SQL Server firewall rules
   - Verify database permissions

3. **Import Failures**
   - Check Parquet file integrity
   - Validate schema compatibility
   - Review SQL Server logs

4. **Performance Issues**
   - Use chunked processing for large files
   - Optimize database indexes
   - Consider Spark for very large datasets

## Monitoring

Monitor imports through:
- Console output
- Log files
- SQL Server DMVs
- Azure Monitor (if deployed in Azure)
- Application Insights

## Security

- **Secrets**: Never commit `.env` files
- **AAD**: Use Azure AD authentication
- **RBAC**: Implement least-privilege access
- **Encryption**: Ensure data encryption in transit
- **Auditing**: Enable SQL Server auditing

## Integration

This Python project integrates with:
- Azure SQL Database
- Azure Data Lake (for Parquet files)
- Azure AD for authentication
- Azure Synapse Analytics (optional)
- Apache Spark clusters

## Deployment

Deploy to:
- **Azure Data Factory**: As pipeline activities
- **Azure Databricks**: As notebook jobs
- **Azure Synapse**: As Spark jobs
- **Azure Functions**: As scheduled imports
- **VM/Container**: As scheduled tasks

## Future Enhancements

Potential improvements:
- Delta Lake integration
- Real-time streaming imports
- Data quality checks
- Automated schema evolution
- Incremental loads
- Change data capture
- Data lineage tracking

## Support

For issues or questions:
- Review logs for error messages
- Check database connectivity
- Verify Parquet file format
- Contact the development team

## Additional Notes

- Ensure appropriate ODBC driver for your OS
- Use Azure AD for secure authentication
- Implement comprehensive logging
- Follow error handling best practices
- Optimize for performance with large datasets
- Test thoroughly in dev before prod deployment
