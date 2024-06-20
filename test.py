import requests

def get_total_referring_domains(api_token, target):
    url = "https://api.ahrefs.com/v3/site-explorer/refdomains"
    params = {
        "token": api_token,
        "target": target,
        "select": "domain",
        "mode": "domain",
        "output": "json",
        "limit": "1000",  # Adjust as needed
        "history": "all_time"  # Include full history by default
    }

    response = requests.get(url, params=params)
    
    if response.status_code == 200:
        data = response.json()
        total_referring_domains = len(data['refdomains'])
        return total_referring_domains
    else:
        print(f"Error: {response.status_code}, {response.text}")
        return None

# Example usage:
api_token = "An2SfxX75sbt6HRdX1iqbkRoSI6bZQgjO_Hu_wmv"
target = "bairesdev.com"
total_referring_domains = get_total_referring_domains(api_token, target)
print(f"Total referring domains for {target}: {total_referring_domains}")
