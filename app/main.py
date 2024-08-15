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

        try:
            # Get the memory string from settings manager
            memory = settings_manager.all_records_to_string()
        except Exception as e:
            logger.error(f"Error occurred while fetching memory records: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while fetching memory records", "error": str(e)}
            )

        try:
            # Calculate the total tokens based on memory
            tokens_for_memory = settings_manager.num_tokens(memory)
            total_tokens += tokens_for_memory
        except Exception as e:
            logger.error(f"Error occurred while calculating tokens: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while calculating tokens", "error": str(e)}
            )

        try:
            # Log before initializing the Decision class
            logger.info("Initializing Decision class.")

            # Initialize Decision class
            decisions = Decision(memory)

        except Exception as e:
            logger.error(f"Error occurred while initializing Decision class: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while initializing Decision class", "error": str(e)}
            )

        try:
            # Log before calling get_next_action
            logger.info("Calling get_next_action on Decision class.")

            # Get the next action from Decision class
            next_action = decisions.get_next_action()

        except Exception as e:
            logger.error(f"Error occurred while getting next action: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while getting next action", "error": str(e)}
            )

        try:
            # Parse the next action JSON string into a dictionary
            next_action_dict = json.loads(next_action)
            action = next_action_dict.get("action")
            observation = next_action_dict.get("observation")
        except json.JSONDecodeError as e:
            logger.error(f"Error occurred while parsing JSON response: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while parsing JSON response", "error": str(e)}
            )
        except KeyError as e:
            logger.error(f"Missing expected key in the action response: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Missing expected key in the action response", "error": str(e)}
            )
        except Exception as e:
            logger.error(f"Error occurred while processing the next action: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while processing the next action", "error": str(e)}
            )

        # Logging the action, observation, and tokens
        try:
            logger.info(
                f"\n\n============= \n"
                f"TOKENS: {tokens_for_memory}\n"
                f"TOTAL TOKENS: {total_tokens}\n"
                f"=============\n"
                f"ACTION: {action}\n"
                f"OBSERVATION: {observation}\n"
                f"=============\n"
                f"MESSAGE: {message}\n"
                f"=============\n"
            )
            return action, observation
        except Exception as e:
            logger.error(f"Error occurred while logging action and observation: {str(e)}")
            return JSONResponse(
                status_code=500,
                content={"message": "Error occurred while logging action and observation", "error": str(e)}
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
        # Clear the logs and objectives
        settings_manager.reset_record("logs")
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


