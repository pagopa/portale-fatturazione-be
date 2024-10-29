#import_check_schema.py
import os
import sys
import pandas as pd
import pyarrow.parquet as pq
import logging   
from dotenv import load_dotenv  

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s') 

dir = os.getcwd()   
scripts_path = os.path.abspath(os.path.join(dir, 'Scripts'))
sys.path.append(scripts_path)

from pagoPA_io import  * # type: ignore # noqa 
from pagoPA_db import  * # type: ignore # noqa 
from pagoPA_utils import * # type: ignore # noqa 

file1  = sys.argv[1]  
file2  = sys.argv[2]

if len(sys.argv) > 1 and sys.argv[1].strip():
    logging.info(f"file1: {sys.argv[1]}")
else:
    logging.info("Usage: python import_import_check_schema.py <file1> <file2>.")

if len(sys.argv) > 2 and sys.argv[2].strip():
    logging.info(f"file2: {sys.argv[2]}")
else:
    logging.info("Usage: python import_import_check_schema.py <file1> <file2>.")

 
try:
    # Read the schema of each file
    schema1 = pq.read_schema(file1)
    schema2 = pq.read_schema(file2)
      
    # Print the schemas
    print("Schema of file 1:")
    print(schema1)

    print("\nSchema of file 2:")
    print(schema2)

    # Compare schemas
    if schema1 == schema2:
        print("\nSchemas are identical.")
    else:
        print("\nSchemas are different.")      

    compare_schemas(schema1, schema2, file1, file2)   # type: ignore # noqa 
    
    logging.info("Processo completato con successo.")  

except Exception as e:
    logging.error(f"Error: {e}") 
    raise   
