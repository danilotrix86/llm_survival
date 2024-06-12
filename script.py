import logging
from fastapi import FastAPI, HTTPException
from validation.pydantic_val import ActionRequest
from Brain.memory import Memory
from settings.utils import load_categories_from_json
from Brain.decisions import Decision

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

        # Get next action
        decisions = Decision(memory)
        next_action = decisions.get_next_action()
        
        logger.info(f"Next action: {next_action}")
        
        return next_action
    
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        raise HTTPException(status_code=400, detail=str(e))
