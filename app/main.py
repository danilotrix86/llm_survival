import logging
from fastapi import FastAPI, Body, HTTPException
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

# Global variable to store the total tokens consumed
total_tokens = 0

@app.get("/messages/")
def get_messages():
    messages_file = "app/game_settings/messages.json"
    try:
        loadjson = load_from_json("messages", messages_file)
        if loadjson:
            logger.info(f"Messages loaded successfully")
            return loadjson
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        return JSONResponse(status_code=500, content={"message":str(e)})
    

@app.get("/xp/")
def get_messages():
    messages_file = "app/game_settings/xp.json"
    try:
        return load_from_json("xp", messages_file)
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

    global total_tokens  # Declare the use of the global variable
    
    # Log the received request data
    logger.info(f"Received request: {action_request}")

    settings_manager = SettingsManager(settings_dir="app/settings")
    
    # check and update the objectives
    objectives = settings_manager.updateObjectives(action_request.inventory)

    # Update memory with the received action request
    message = settings_manager.update_memory(action_request)

    # Process based on the configured approach
    if config.APPROACH == "ZEROSHOT":

        memory = settings_manager.all_records_to_string()
        total_tokens = total_tokens + settings_manager.num_tokens(memory)

        try:
            # Get the next action from Decision class
            decisions = Decision(memory)
            next_action = decisions.get_next_action()
            #logger.info(f"Next action: {next_action}")
            # Parse the next action JSON string into a dictionary
            next_action_dict = json.loads(next_action)
            action = next_action_dict.get("action")
            observation = next_action_dict.get("observation")
            logger.info (f"\n\n============= \nTOKENS: {settings_manager.num_tokens(memory)}\nTOTAL TOKENS: {total_tokens}\n=============\nACTION: {action}\nOBSERVATION: {observation}\n=============\nMESSAGE: {message}\n=============\n")
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

@app.post("/start_new_game/")
def start_new_game():
    """
    Endpoint to start a new game.

    Returns:
    - message: The message to be displayed to the user
    """

    settings_manager = SettingsManager(settings_dir="app/settings")

    try:
        # Clear the logs, current_plan, warnings and game_info
        settings_manager.reset_record("logs")
        settings_manager.reset_record("game_info")
        settings_manager.reset_record("objectives")

        # Reset the inventory quantities
        settings_manager.reset_inventory_quantities()

        # Reset the player info
        settings_manager.set_player_info_to_very_good()

        # Add the first objective
        first_objective = {
            "name": "Build Shelter",
            "description": "Build a shelter to protect yourself from the elements, have fire and a place to sleep."
        }
        settings_manager.add_item('objectives', first_objective)

        return {"message": "New game started successfully"}
    
    except ValueError as e:
        logger.error(f"ValueError: {e}")
        raise HTTPException(status_code=400, detail=str(e))
    except RuntimeError as e:
        logger.error(f"RuntimeError: {e}")
        raise HTTPException(status_code=500, detail=str(e))
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        raise HTTPException(status_code=500, detail="An unexpected error occurred")


@app.get("/xp/")
def get_messages():
    messages_file = "app/game_settings/xp.json"
    try:
        return load_from_json("xp", messages_file)
    except Exception as e:
        logger.error(f"Error processing request: {e}")
        return JSONResponse(status_code=500, content={"message":str(e)})