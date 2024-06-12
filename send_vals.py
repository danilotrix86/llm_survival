# send_request.py

import requests

url = "http://127.0.0.1:8000/next_action/"

# Define the payload as a dictionary
payload = {
    "action": "pick_berry",
    "status": "success",
    "message": "You found some delicious berries and you picked them up!",
    "inventory": {
        "shelter": 1,
        "food": 0,
        "water": 0,
        "fish": 0,
        "berry": 20,
        "stick": 10,
        "wood": 0,
        "stone": 0,
        "fiber": 0,
        "ax": 0,
        "firecamp": 0,
        "raft": 0
    },
    "player_info": {
        "health": "Gianluca",
        "hunger": "Very Good",
        "thirst": "Good",
        "energy": "Very low"
    }
}

# Send the POST request
response = requests.post(url, json=payload)

# Print the response from the server
print(response.status_code)
print(response.json())
