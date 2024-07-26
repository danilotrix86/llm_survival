import requests

url = "http://127.0.0.1:8000/start_new_game/"

# Define the payload as a dictionary
payload = {
    "action": "pick_stones",
    "status": "success",
    "message": "You just picked up some stones.",
    "inventory": {
        "axe": 0,
        "stick": 4,
        "wood": 0,
        "stone": 4,
        "fibers": 0,
        
    },
    "player_info": {
        "health": "Very Good",
        "hunger": "Very Good",
        "thirst": "Very Good",
        "stress": "Very Good"
    }
}

# Send the POST request with JSON payload
response = requests.post(url)

# Print the response from the server
print(response.status_code)
try:
    print(response.json())
except requests.exceptions.JSONDecodeError:
    print(response.text)
