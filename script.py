import logging
from fastapi import FastAPI, HTTPException
from validation.pydantic_val import ActionRequest
from Brain.memory import Memory
from settings.utils import load_categories_from_json
from Brain.decisions import Decision
import json

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI()

# Load categories file path
categories_file = 'Brain/memory.json'

# Initialize memory with categories
memory = Memory(load_categories_from_json(categories_file), 'settings')
logger.info(f"Memory initialized: {memory.to_string()}")

@app.post("/next_action/")
def get_next_action(action_request: ActionRequest):
    try:
        # Log the received request data
        logger.info(f"Received request: {action_request}")

        # Update memory with the action request
        memory.update_memory(action_request)

        logger.info(f"Memory tokens: {memory.num_tokens()}")
        # Get next action
        decisions = Decision(memory)
        next_action = decisions.get_next_action()
        # Parse the string into a dictionary
        next_action_dict = json.loads(next_action)

        # Extract the action and reason
        action = next_action_dict.get("action")
        reason = next_action_dict.get("reason")

        logger.info(f"Next action: {action}, Reason: {reason}") 


        return action, reason
    
    
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        raise HTTPException(status_code=400, detail=str(e))
