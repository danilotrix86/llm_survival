import requests

url = "http://127.0.0.1:8000/next_action/"

# Define the payload as a dictionary
payload = {
    "action": "craft_raft",
    "status": "failed",
    "message": "You wanted to craft a raft but you don't have enough materials. You need 20 woods, 20 sticks and 10 ropes",
    "inventory": {
        "axe": 5,
        "stick": 3,
        "wood": 5,
        "stone": 100,
        "fibers": 2,
        
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
