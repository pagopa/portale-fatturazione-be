#import_contracts.py
import os
import sys
import pandas as pd
import pyodbc  
import logging 
from azure.identity import InteractiveBrowserCredential 
from dotenv import load_dotenv  

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s') 
 
dir = os.getcwd()   
scripts_path = os.path.abspath(os.path.join(dir, 'Scripts'))
sys.path.append(scripts_path) 

environment = sys.argv[1]  
parquet_file = sys.argv[2]

if len(sys.argv) > 1 and sys.argv[1].strip():
    logging.info(f"env: {sys.argv[1]}")
else:
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_contracts.py dev/uat/prod file_name")

if len(sys.argv) > 2 and sys.argv[2].strip():
    logging.info(f"parquet file: {sys.argv[2]}")
else:
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_contracts.py dev/uat/prod file_name")

 
if environment == 'prod':
    load_dotenv('.env.prod')
elif environment == 'stage':
    load_dotenv('.env.stage')
elif environment == 'dev':
    load_dotenv('.env.dev')    
else:
    load_dotenv('.env')    
 
server = os.getenv('SERVER')
database = os.getenv('DATABASE')
table = os.getenv('TABLE_CONTRACTS')

connection_string = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={server};DATABASE={database};Authentication=ActiveDirectoryInteractive;'
 
try:
    dir = os.getcwd()   
    df = pd.read_parquet(parquet_file)     
    logging.info(f"DataFrame Columns: {df.columns.tolist()}")
    logging.info(f"DataFrame Shape: {df.shape}")
    df['signed_date'] = pd.to_datetime(df['signed_date'], errors='coerce') 
    if df['signed_date'].isnull().any():
        print("Warning: Some dates could not be converted. These will be inserted as NULL.") 
 
    conn = pyodbc.connect(connection_string)
    cursor = conn.cursor()
    for index, row in df.iterrows(): 
        sql_merge = f'''
        MERGE INTO {table} AS target
        USING (SELECT ? AS contract_id) AS source
        ON target.contract_id = source.contract_id
        WHEN MATCHED THEN
            UPDATE SET 
                document_name = ?,
                provider_names = ?,
                signed_date = ?,
                contract_type = ?,
                name = ?,
                abi = ?,
                tax_code = ?,
                vat_code = ?,
                vat_group = ?,
                pec_mail = ?,
                courtesy_mail = ?,
                referentefattura_mail = ?,
                sdd = ?,
                sdi_code = ?,
                membership_id = ?,
                recipient_id = ?,
                year_month = ?
        WHEN NOT MATCHED THEN
            INSERT (contract_id, document_name, provider_names, signed_date, contract_type, 
                    name, abi, tax_code, vat_code, vat_group, pec_mail, 
                    courtesy_mail, referentefattura_mail, sdd, sdi_code, 
                    membership_id, recipient_id, year_month)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        '''
                
        values = (
            row['contract_id'], row['document_name'], 
            row['provider_names'] if pd.notnull(row['provider_names']) else None,   
            row['signed_date'] if pd.notnull(row['signed_date']) else None,  
            row['contract_type'], row['name'], 
            row['abi'] if pd.notnull(row['abi']) else None, 
            row['tax_code'], row['vat_code'], 
            row['vat_group'], row['pec_mail'], row['courtesy_mail'], 
            row['referentefattura_mail'] if pd.notnull(row['referentefattura_mail']) else None,  
            row['sdd'], 
            row['sdi_code'] if pd.notnull(row['sdi_code']) else None,  
            row['membership_id'], 
            row['recipient_id'], row['year_month'],
            # INSERT
            row['contract_id'], row['document_name'], 
            row['provider_names'] if pd.notnull(row['provider_names']) else None,   
            row['signed_date'] if pd.notnull(row['signed_date']) else None,  
            row['contract_type'], row['name'], 
            row['abi'] if pd.notnull(row['abi']) else None, 
            row['tax_code'], row['vat_code'], 
            row['vat_group'], row['pec_mail'], row['courtesy_mail'], 
            row['referentefattura_mail'] if pd.notnull(row['referentefattura_mail']) else None,  
            row['sdd'], 
            row['sdi_code'] if pd.notnull(row['sdi_code']) else None,  
            row['membership_id'], 
            row['recipient_id'], row['year_month'] 
        )
 
        logging.info(f"Inserting row {index}: {values}") 
        logging.info("Length of values:", len(values))  
        cursor.execute(sql_merge, values)
 
    conn.commit()
    logging.info("Data inserted successfully.")

except Exception as e:
    logging.error(f"Error: {e}") 
    raise  
finally: 
    cursor.close()
    conn.close()
