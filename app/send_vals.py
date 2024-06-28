import requests

url = "http://127.0.0.1:8000/next_action/"

# Define the payload as a dictionary
payload = {
    "action": "eat",
    "status": "success",
    "message": "You ate some berry from the tree. It was good",
    "inventory": {
        "axe": 12,
        "stick": 8,
        "wood": 7,
        "stone": 0,
        "fibers": 0,
        
    },
    "player_info": {
        "health": "Very Low",
        "hunger": "Very Low",
        "thirst": "Very Good",
        "energy": "Very Good"
    }
}

# Send the POST request with JSON payload
response = requests.post(url, json=payload)

# Print the response from the server
print(response.status_code)
try:
    print(response.json())
except requests.exceptions.JSONDecodeError:
    print(response.text)
