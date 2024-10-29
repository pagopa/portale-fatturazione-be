# pagoPA_io.py
import dask.dataframe as dd 
import os 
import difflib
import logging
import pandas as pd

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s') 

__all__ = ['load_parquet', 'compare_schemas', 'load_parquet_pd'] 
  
def load_parquet(parquet_file): 
    if not os.path.exists(parquet_file):
        raise FileNotFoundError(f"The file {parquet_file} does not exist.") 
    logging.info(f"Processing file: {parquet_file}")    
    df = dd.read_parquet(parquet_file) 
    df = df.compute() 
    return df   
 
def load_parquet_pd(parquet_file): 
    if not os.path.exists(parquet_file):
        raise FileNotFoundError(f"The file {parquet_file} does not exist.")  
    logging.info(f"Processing file: {parquet_file}")    
    return pd.read_parquet(parquet_file) 

def compare_schemas(schema1, schema2, file1_name='File 1', file2_name='File 2'):
    """
    Compare two PyArrow schemas and print the differences line-by-line.

    Parameters:
        schema1 (pyarrow.Schema): The first schema to compare.
        schema2 (pyarrow.Schema): The second schema to compare.
        file1_name (str): Name of the first file for display purposes.
        file2_name (str): Name of the second file for display purposes.
    """
    # Convert schema to string for comparison
    schema1_str = str(schema1).splitlines()
    schema2_str = str(schema2).splitlines()

    # Show the differences
    diff = difflib.unified_diff(schema1_str, schema2_str, fromfile=file1_name, tofile=file2_name, lineterm='')

    print("\nDifferences in schemas:")
    for line in diff:
        print(line)
