#import_kpmg.py
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

from pagoPA_io import  * # type: ignore # noqa 
from pagoPA_db import  * # type: ignore # noqa 
from pagoPA_utils import * # type: ignore # noqa 

environment = sys.argv[1]  
parquet_file = sys.argv[2]

if len(sys.argv) > 1 and sys.argv[1].strip():
    logging.info(f"env: {sys.argv[1]}")
else:
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_kpmg.py dev/uat/prod file_name")

if len(sys.argv) > 2 and sys.argv[2].strip():
    logging.info(f"parquet file: {sys.argv[2]}")
else:
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_kpmg.py dev/uat/prod file_name")

 
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
table = os.getenv('TABLE_KPMG')
table_anagrafica = os.getenv('TABLE_CONTRACTS')
values = table.split(".")
schema = values[0]
table_name =  values[1]
table_anagrafica_name  = table_anagrafica.split(".")[1]
 
connection_string = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={server};DATABASE={database};Authentication=ActiveDirectoryInteractive;'
 
def _process_pd(df):
    df.rename(columns={'indirizzo pec': 'indirizzo_pec'}, inplace=True)  # modifico indirizzo pec
    df['descrizione_riga'] = df['descrizione_riga'].apply(lambda x: extract_uris(x) if x is not None else None) # type: ignore # inserisco solo le uri 
    # Assuming 'data' is a column with date strings
    df['data'] = pd.to_datetime(df['data'], errors='coerce')  # Converts to datetime, coercing errors
    df['riferimento_data'] = pd.to_datetime(df['riferimento_data'], errors='coerce')  
    df['codice_articolo'] = df['codice_articolo'].method('') # Replace NULL with an empty string

__key_columns = ['contract_id', 'year_quarter', 'numero', 'progressivo_riga', 'codice_articolo']


def _find_duplicates_pd(df): 
    duplicates = df[df.duplicated(subset=__key_columns, keep=False)]  # keep=False to show all duplicates   
    if not duplicates.empty:
        logging.info(f"Total duplicate combinations found: {len(duplicates)}") 
        duplicates.to_csv(r'data\duplicates.csv', index=False)  # index=False to avoid saving the index as a column  
        duplicates.to_parquet(r'data\duplicates.parquet', index=False) 
        logging.info(r"Duplicate rows saved to data\.")   
    else:
        logging.info("No duplicate combinations found.") 
    duplicates    

def _find_null_contract_id_pd(df): 
    # Filter rows where 'contract_id' is null
    df_null_contract_id = df[df['contract_id'].isnull()] 
    if not df_null_contract_id.empty:
        logging.info(f"Total duplicate combinations found: {len(df_null_contract_id)}") 
        df_null_contract_id.to_csv(r'data\null_contract_id.csv', index=False)  # index=False to avoid saving the index as a column  
        df_null_contract_id.to_parquet(r'data\null_contract_id.parquet', index=False)   
    else:
        logging.info("No duplicate combinations found.") 
    df_null_contract_id    

def _find_kpmg_nulls_pd(df): 
    df_kpmg_nulls = df[df['contract_id'].isna()]
    if not df_kpmg_nulls.empty:
        logging.info(f"Total duplicate combinations found: {len(df_kpmg_nulls)}") 
        df_kpmg_nulls.to_csv(r'data\kpmg_nulls.csv', index=False)  # index=False to avoid saving the index as a column  
        df_kpmg_nulls.to_parquet(r'data\kpmg_nulls.parquet', index=False)   
    else:
        logging.info("No duplicate combinations found.") 
    df_kpmg_nulls   

try: 
    df = load_parquet_pd(parquet_file) # type: ignore 
    _process_pd(df) #process data frame 
    df_duplicates = _find_duplicates_pd(df) #find duplicates
    df_contract_id_null = _find_null_contract_id_pd(df) #find contract_id null 

    conn = pyodbc.connect(connection_string) 
    verify_connection_to_db(conn)  # type: ignore 
    create_table_kpmg(schema, table_name, conn) # type: ignore #creo la table se non esiste 
    get_contract_id_by_vat_code(df_contract_id_null, schema, table_anagrafica_name, conn) # type: ignore 
    
    df_kpmg_nulls = _find_kpmg_nulls_pd(df_contract_id_null) #find kpmg without correlation vat contract_id
    df_recovered= pd.concat([df_contract_id_null, df_kpmg_nulls]).drop_duplicates(keep=False) # Concatenate and drop duplicates to keep only rows in `df1` not in `df2`
    insert_update_dataframe_to_sql(df_recovered, schema, table_name, conn, key_columns) # type: ignore # aggiorno i dati in append
 
    df_left= pd.concat([df, df_recovered]).drop_duplicates(keep=False)
    insert_update_dataframe_to_sql(df_left, schema, table_name, conn, key_columns) # type: ignore # aggiorno i dati in append 
    
    logging.info("Processo completato con successo.")  

except Exception as e:
    logging.error(f"Error: {e}") 
    raise   
