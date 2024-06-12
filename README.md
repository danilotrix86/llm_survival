# LLM Survival

This repository contains a project that simulates a survival game using a language model. Follow the instructions below to set up and run the project.

## Setup

Create a virtual environment folder:

```bash
python -m venv venv
```

Activate the virtual environment:


On Windows:
```bash
venv\Scripts\activate
```
On Unix or macOS:
```bash
source venv/bin/activate
```
Install the required dependencies:

```bash
pip install -r requirements.txt
```

Create a .env file and add your OpenAI API key:
```bash
OPENAI_API_KEY=your_api_key_here
```

## Running the Server
Launch the server with the following command:
```bash
uvicorn script:app --reload
```

## Endpoint

The endpoint for the server is http://127.0.0.1:8000/next_action/.

To send a request, define the payload as a dictionary:
```bash
pythonCopy codepayload = {
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
```
