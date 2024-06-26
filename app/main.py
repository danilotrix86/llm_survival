import logging
from fastapi import FastAPI, Body
from app.validation.pydantic_val import ActionRequest  # Pydantic models for request and response
from app import config  # Configuration settings
from fastapi.responses import JSONResponse  # JSON response for error handling
from app.settings.settings_manager import SettingsManager
from services.decisions import Decision
import json
from app.helper.utils import load_from_json

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI()

@app.get("/messages/")
def get_messages():
    messages_file = "game_settings/messages.json"
    try:
        return load_from_json("messages", messages_file)
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        return JSONResponse(status_code=500, content={"message":str(e)})
    

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

    # Log the received request data
    logger.info(f"Received request: {action_request}")

    settings_manager = SettingsManager(settings_dir="app/settings")

    # Update memory with the received action request
    settings_manager.update_memory(action_request)

    # Process based on the configured approach
    if config.APPROACH == "ZEROSHOT":

        memory = settings_manager.all_records_to_string()
        logger.info(f"Memory tokens: {settings_manager.num_tokens(memory)}")

        try:
            # Get the next action from Decision class
            decisions = Decision(memory)
            next_action = decisions.get_next_action()
            logger.info(f"Next action: {next_action}")
            # Parse the next action JSON string into a dictionary
            next_action_dict = json.loads(next_action)
            action = next_action_dict.get("action")
            observation = next_action_dict.get("observation")
            logger.info (f"Action: {action}, Observation: {observation}")
            return action, observation
        
        except Exception as e:
            # Log the error
            logger.error(f"Error occurred while getting next action: {str(e)}")
            # Return an error response
            return JSONResponse(
                status_code=500,
                content={
                    "message": "An error occurred while making a new decision",
                    "error": str(e)
                }
            )
    elif config.APPROACH == "AGENTIC":
        from services.agent import SurvivalGameAgent

        agent = SurvivalGameAgent()
        agent.initialize_agent()

        # Input data for the agent
        input_data = {
            "input": "Return only a JSON object with the next action and observation (no other information added). The action should be only 1 of the actions in the actions book. You can't use other actions",
            "chat_history": []
        }

        try:
            action, observation = agent.execute_agent(input_data)
            logger.info (f"Action: {action}, Observation: {observation}")
            return action, observation
        
        except Exception as e:
            logger.error(f"Error occurred while executing agent: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={
                    "message": "An error occurred while executing the agent",
                    "error": str(e)
                }
            )
