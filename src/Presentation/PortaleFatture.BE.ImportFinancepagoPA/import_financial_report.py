#import_financial_report.py 
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
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_financial_report.py dev/uat/prod file_name")

if len(sys.argv) > 2 and sys.argv[2].strip():
    logging.info(f"parquet file: {sys.argv[2]}")
else:
    logging.info("Usage: python import_contracts.py <env> <filename>. i.e. import_financial_report.py dev/uat/prod file_name")

 
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
table = os.getenv('TABLE_FINANCIAL_REPORTS')
values = table.split(".")
schema = values[0]
table_name =  values[1]
 
connection_string = f'DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={server};DATABASE={database};Authentication=ActiveDirectoryInteractive;'
try:
    dir = os.getcwd()   
    logging.info(f"Processing file: {parquet_file}")    
    df = pd.read_parquet(parquet_file) 

    logging.info(f"DataFrame Columns: {df.columns.tolist()}")
    logging.info(f"DataFrame Shape: {df.shape}")
    conn = pyodbc.connect(connection_string) 
    verify_connection_to_db(conn)  # type: ignore

    create_table_financial_report(schema, table_name, conn) # type: ignore #creo la table se non esiste 
    primary_keys = ['recipient_id','codice_articolo','year_quarter'] # type: ignore   #specifico la primary key
    insert_update_dataframe_to_sql(df, schema, table_name, conn, primary_keys) # type: ignore # aggiorno i dati in append, c'è la
        # PK recipient_id, codice_articolo, year_quarter 
    logging.info("Processo completato con successo.")  

except Exception as e:
    logging.error(f"Error: {e}") 
    raise   