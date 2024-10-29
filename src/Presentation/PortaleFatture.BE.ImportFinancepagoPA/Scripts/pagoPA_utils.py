# pagoPA_utils.py
import re

def extract_uris(text): 
    if text is None:
     return None
    uri_pattern = r'(https?://[^\s]+)'  
    uris = re.findall(uri_pattern, text) 
    return ','.join(uris)