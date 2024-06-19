import requests

url = "http://127.0.0.1:8000/next_action/"

# Define the payload as a dictionary
payload = {
    "action": "eat",
    "status": "success",
    "message": "You ate some berry from the tree. It was good",
    "inventory": {
        "shelter": 2,
        "food": 0,
        "water": 0,
        "fish": 0,
        "berry": 15,
        "stick": 8,
        "wood": 7,
        "stone": 0,
        "fiber": 0,
        "ax": 1,
        "firecamp": 0,
        "raft": 0
    },
    "player_info": {
        "health": "Very Good",
        "hunger": "Very Good",
        "thirst": "Very Good",
        "energy": "Good"
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
