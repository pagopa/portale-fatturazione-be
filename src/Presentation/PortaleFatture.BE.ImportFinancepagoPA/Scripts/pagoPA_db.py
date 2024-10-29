# pagoPA_db.py
import logging 
from sqlalchemy import *
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

def _getEngine(connection):
    return create_engine(
        "mssql+pyodbc://",
        creator=lambda: connection)

def verify_connection_to_db(connection):  
    engine = _getEngine(connection) 
    with engine.connect() as connection:
        logging.info("Connection successful!")  

def get_contract_id_by_vat_code(df, schema, table_name, connection):
    engine = _getEngine(connection)
    metadata = MetaData( schema = schema)
    metadata.reflect(bind=engine)  
    table = Table(table_name, metadata, autoload_with=engine)
    Session = sessionmaker(bind=engine)
    with Session() as session:
        for index, row in df.iterrows():
            vat_code = row['vat_code']
            query = select(table.c.contract_id).where(table.c.vat_code == vat_code)
            result = session.execute(query).fetchone() 
            if result is not None:
              logging.info(f"Contract Id: {result[0]}")  
            df.loc[index, 'contract_id'] = result[0] if result else None 


def insert_update_dataframe_to_sql(df, schema, table_name, connection, primary_keys, handle_errors=false): 
    engine = _getEngine(connection)
    metadata = MetaData( schema = schema)
    metadata.reflect(bind=engine)  
    batch_size = 10

    logging.info(f"Starting to upsert {len(df)} records into {table_name} with batch size {batch_size}.")

    total_inserted = 0
    total_updated = 0

    table = Table(table_name, metadata, autoload_with=engine)
    with engine.begin() as connection:
        for start in range(0, len(df), batch_size):
            batch = df.iloc[start:start + batch_size]
            for index, row in batch.iterrows(): 
                stmt = insert(table).values(row.to_dict()) 
                try:
                    connection.execute(stmt)
                    total_inserted += 1  
                    logging.info(f"totale: {total_inserted} su {len(df)}")  
                except Exception as e:   
                    if "UNIQUE constraint failed" in str(e) or "duplicate key" in str(e) or "Violation of PRIMARY KEY constraint" in str(e):
                        conditions = [table.c[key] == row[key] for key in primary_keys]
                        where_clause = conditions[0]
                        for condition in conditions[1:]:
                            where_clause = where_clause & condition
 
                        update_stmt = update(table).where(where_clause).values(row.to_dict())
                        connection.execute(update_stmt)
                        total_updated += 1
                        logging.info(f"totale: {total_updated} su {len(df)}")  
                    else:
                        logging.error(f"An unexpected error occurred: {e}")  
 
    total_records_processed = total_inserted + total_updated
    if handle_errors == true:
        if total_records_processed == len(df):
            logging.info(f"Success: All {len(df)} records inserted/upserted into {table_name}.")
        else:
            logging.warning(f"Warning: Expected {len(df)} records, but only {total_records_processed} were inserted/upserted into {table_name}.")
            logging.info(f"Total Inserted: {total_inserted}, Total Updated: {total_updated}.")
            raise Exception(f"Rollback: Expected {len(df)} records, but only {total_records_processed} were processed.") 
    logging.info(f"Total Inserted: {total_inserted}, Total Updated: {total_updated}.")
 
def create_table_kpmg(schema:str, table_name: str, connection):
    engine = _getEngine(connection)
    metadata = MetaData(schema=schema) 
 
    Table(table_name, metadata,
        Column('contract_id', NVARCHAR(25), nullable=False, primary_key=True),
        Column('tipo_doc', NVARCHAR(), nullable=True),
        Column('sezionale', NVARCHAR(), nullable=True),
        Column('codice_aggiuntivo', NVARCHAR(), nullable=True),
        Column('denominazione_destinatario', NVARCHAR(), nullable=True),
        Column('indirizzo', NVARCHAR(), nullable=True),
        Column('citta', NVARCHAR(), nullable=True),
        Column('prov', NVARCHAR(), nullable=True),
        Column('cap', NVARCHAR(), nullable=True),
        Column('stato', NVARCHAR(), nullable=True),
        Column('vat_code', NVARCHAR(), nullable=True),
        Column('cod_fisc', NVARCHAR(), nullable=True),
        Column('valuta', NVARCHAR(), nullable=True),
        Column('id', Integer, nullable=True),
        Column('numero', NVARCHAR(), nullable=True),
        Column('data', DateTime, nullable=True),
        Column('bollo', NVARCHAR(), nullable=True),
        Column('codifica_art', NVARCHAR(), nullable=True),
        Column('progressivo_riga', Integer, nullable=False, primary_key=True),   
        Column('codice_articolo', NVARCHAR(), nullable=True),
        Column('descrizione_riga', NVARCHAR(), nullable=True),
        Column('prezzo_unit', NVARCHAR(), nullable=True),
        Column('q_ta', Integer, nullable=True),
        Column('importo', DECIMAL(38, 14), nullable=True),
        Column('cod_iva', NVARCHAR(), nullable=True),
        Column('percent_iva', NVARCHAR(), nullable=True),
        Column('iva', NVARCHAR(), nullable=True),
        Column('condizioni', NVARCHAR(), nullable=True),
        Column('causale', NVARCHAR(), nullable=True),
        Column('ind_tipo_riga', Integer, nullable=True),
        Column('codice_sdi', NVARCHAR(), nullable=True),
        Column('num_doc_rif', NVARCHAR(), nullable=True),
        Column('data_doc_rif', NVARCHAR(), nullable=True),
        Column('tipo_doc_rif', NVARCHAR(), nullable=True),
        Column('tipo_dato', NVARCHAR(), nullable=True),
        Column('riferimento_testo', NVARCHAR(), nullable=True),
        Column('riferimento_numero', NVARCHAR(), nullable=True),
        Column('riferimento_data', DateTime, nullable=True),
        Column('anno', NVARCHAR(), nullable=True),
        Column('rif_fattura', NVARCHAR(), nullable=True),
        Column('sconto', NVARCHAR(), nullable=True),
        Column('data_limite_pagamento', NVARCHAR(), nullable=True),
        Column('banca', NVARCHAR(), nullable=True),
        Column('iban_riferimento_per_pagamento', NVARCHAR(), nullable=True),
        Column('cig', NVARCHAR(), nullable=True),
        Column('indirizzo_pec', NVARCHAR(), nullable=True),
        Column('cup', NVARCHAR(), nullable=True),
        Column('tipo_doc_rif1', NVARCHAR(), nullable=True),
        Column('year_quarter', NVARCHAR(10), nullable=False, primary_key=True)
    )
 
    metadata.create_all(engine)
    logging.info(f"Table {schema}.{table_name} created successfully (if it did not exist).")   

def create_table_financial_report(schema:str, table_name: str, connection):
    engine = _getEngine(connection)
    metadata = MetaData(schema=schema) 
 
    Table(
        table_name, metadata,
        Column('abi', NVARCHAR(), nullable=True),
        Column('recipient_id', NVARCHAR(20), nullable=false),
        Column('name', NVARCHAR(), nullable=True),
        Column('category', NVARCHAR(), nullable=True),
        Column('current_trx', NVARCHAR(), nullable=True),
        Column('value', DECIMAL(38, 14), nullable=True),
        Column('codice_articolo', NVARCHAR(), nullable=True),
        Column('year_quarter', NVARCHAR(10), nullable=True)
    )
 
    metadata.create_all(engine)
    logging.info(f"Table {schema}.{table_name} created successfully (if it did not exist).")       