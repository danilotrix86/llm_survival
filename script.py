import logging
from fastapi import FastAPI, Body
from validation.pydantic_val import ActionRequest  # Importing Pydantic model for request validation
from zeroshot.memory import Memory  # Importing Memory class
from settings.utils import load_categories_from_json, load_from_json
from zeroshot.decisions import Decision  # Importing Decision class
import json
from config import config  # Configuration settings
from fastapi.responses import JSONResponse  # JSON response for error handling

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI()

# Load categories file path
categories_file = 'zeroshot/memory.json'

# Initialize memory with categories
memory = Memory(load_categories_from_json(categories_file), 'settings')
logger.info(f"Memory initialized: {memory.to_string()}")

@app.post("/next_action/")
def get_next_action(action_request: ActionRequest = Body(...)):
    """
    Endpoint to determine the next action based on the request.

    Parameters:
    - action_request: The request body containing the action details

    Returns:
    - action: The next action to be performed
    - observation: The observation related to the action
    """
    try:
        # Log the received request data
        logger.info(f"Received request: {action_request}")

        # Process based on the configured approach
        if config.APPROACH == "ZEROSHOT":
            # Update memory with the action request
            memory.update_memory(action_request)

            logger.info(f"Memory tokens: {memory.num_tokens()}")

            # Get the next action from Decision class
            decisions = Decision(memory)
            next_action = decisions.get_next_action()

            # Parse the next action JSON string into a dictionary
            next_action_dict = json.loads(next_action)
            action = next_action_dict.get("action")
            observation = next_action_dict.get("observation")

            return action, observation

        elif config.APPROACH == "AGENTIC":
            from agents.agent import SurvivalGameAgent

            agent = SurvivalGameAgent()
            agent.initialize_agent()

            # Input data for the agent
            input_data = {
                "input": "Return only a JSON object with the next action and observation (no other information added). The action should be only 1 of the actions in the actions book. You can't use other actions",
                "chat_history": []
            }

            action, observation = agent.execute_agent(input_data)
            return action, observation

    except Exception as e:
        logger.error(f"Error processing request: {e}")
        return JSONResponse(status_code=500, content={"message": str(e)})


@app.get("/messages/")
def get_messages():
    messages_file = "game_settings/messages.json"
    try:
        return load_from_json("messages", messages_file)
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        return JSONResponse(status_code=500, content={"message":str(e)})