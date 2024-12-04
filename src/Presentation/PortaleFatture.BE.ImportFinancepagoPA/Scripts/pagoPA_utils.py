# pagoPA_utils.py
import re

def extract_uris(text): 
    if text is None:
     return None
    uri_pattern = r'(https?://[^\s]+)'  
    uris = re.findall(uri_pattern, text) 
    return ','.join(uris) 
 
def get_quarter(year_month): 
    year_month = str(year_month).strip()
    year = int(year_month[:4])
    month = int(year_month[4:])
    
    # Determine the period based on the month range
    if month == 1:
        quarter = f"{year-1}_4"  # For January, use the previous year
    elif 2 <= month <= 4:
        quarter = f"{year}_1"
    elif 5 <= month <= 7:
        quarter = f"{year}_2"
    elif 8 <= month <= 10:
        quarter = f"{year}_3"
    else:  # For November (11) and December (12)
        quarter = f"{year}_4" 
    return quarter
