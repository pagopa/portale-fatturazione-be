## 1\. Install Required Libraries:## 

Ensure you have Python installed. Then, install the necessary libraries using pip: 
 

``` Bash
pip install pandas pyodbc azure-identity dotenv logging

``` 

## 2\. Set Up Environment Variables :## 

Create a `.env` file in your project's root directory for each enviroment dev/uat/prod and add the following:

```
SERVER=xxx
DATABASE=xxx
TABLE_CONTRACTS=schema.Table
TABLE_KPMG=schema.Table
TABLE_FINANCIAL_REPORTS=schema.Table
```

## 3\. Project Structure:## 

The project structure looks like this:

```
project/
├── .env.dev
├── import_contracts.py
└── import_kpmg.py
└── ...
``` 

## Running the Scripts:## 

``` Bash
python .\import_kpmg.py prod .\data\kpmg_file.parquet
```

## Additional Notes:## 

-   ## pyodbc:##  Ensure you have the appropriate ODBC driver installed for your database (SQL Server).
-   ## Azure Authentication:##  Using Azure Active Directory authentication, provide your credentials.
-   ## Logging:##  Created logging to track errors and debug issues.
-   ## Error Handling:##  Implemented error handling to gracefully handle exceptions.
-   ## Best Practices:##  Followed Python best practices for code readability, maintainability, and performance.

## For more specific instructions, please contact the team. ## 