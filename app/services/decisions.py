from app import config  # Configuration settings
from pydantic import ValidationError
import logging
from validation.pydantic_val import NextAction

# Initialize the logger
logger = logging.getLogger(__name__)



class Decision:
    """
    Represents the decision-making process for the game character using OpenAI's model.
    """

    def __init__(self, memory):
        """
        Initializes a new Decision instance.

        Parameters:
        -----------
        memory : Memory
            An instance of the Memory class.
        """
        self.memory = memory

        if config.LLM_ENGINE == "openai":
            from services.aiwrapper import OpenAIWrapper
            self.decision_wrapper = OpenAIWrapper(config.GPT_ENGINE)
            logging.info("OpenAI model initialized successfully")
        
        # else if it's one of these: "llama3-8b-8192", "llama3-70b-8192", "llama-3.1-70b-versatile", "llama-3.1-405b-reasoning",  "mixtral-8x7b-32768", "gemma-7b-it", "gemma2-9b-it"
        elif config.LLM_ENGINE in ["llama3-8b-8192", "llama3-70b-8192", "llama-3.1-70b-versatile", "llama-3.1-405b-reasoning",  "mixtral-8x7b-32768", "gemma-7b-it", "gemma2-9b-it"]:
            from services.aiwrapper import GroqWrapper
            self.decision_wrapper = GroqWrapper(config.LLM_ENGINE)
            logging.info("Groq model initialized successfully")

    def get_next_action_old(self):
        """
        Gets the next action from the language model based on the current memory.

        Returns:
        --------
        str
            The next action as a JSON string.
        """

        # Add the memory string as a system message
        self.decision_wrapper.add_message("system", self.memory)

        try:
            # Get the next action from the model
            response = self.decision_wrapper.completion(response_format="json")
            # Extract the content from the response
            response_content = response.choices[0].message.content

            # Validate the response content with Pydantic
            action_response = NextAction.model_validate_json(response_content)

            # Return the validated JSON response
            return action_response.model_dump_json()

        except ValidationError as e:
            # Handle validation errors
            logger.error(f"Validation error: {e.json()}")
            return '{"action": "", "observation": "Validation error"}'

        except Exception as e:
            # Log the error
            logger.error(f"Error fetching next action: {e}")
            return '{"action": "", "observation": "Error fetching next action"}'
        
        
    def get_next_action(self):
        """
        Gets the next action from the language model based on the current memory.

        Returns:
        --------
        str
            The next action as a JSON string.
        """

        # Add the memory string as a system message
        self.decision_wrapper.add_message("system", self.memory)

        try:
            # Get the next action from the model
            response = self.decision_wrapper.completion(response_format="json")

            # Log the raw response type and content for debugging
            logger.info(f"Raw response type: {type(response)}")
            logger.info(f"Raw response content: {response}")

            # Ensure response is not binary
            if isinstance(response, bytes):
                try:
                    # Decode binary data as UTF-8 and log the result
                    response_content = response.decode('utf-8', errors='replace')
                    logger.info(f"Decoded response content (bytes): {response_content}")
                except UnicodeDecodeError as e:
                    logger.error(f"Error decoding response as UTF-8: {str(e)}")
                    return '{"action": "", "observation": "Error decoding response as UTF-8"}'
            else:
                try:
                    # Log the structure of the response if it's not binary
                    response_content = response.choices[0].message.content
                    logger.info(f"Extracted response content (text): {response_content}")
                except AttributeError as e:
                    logger.error(f"Error accessing the response content: {str(e)}")
                    return '{"action": "", "observation": "Error accessing response content"}'

            # Log the raw content before further processing
            logger.info(f"Final response content: {response_content}")

            # Validate and decode the response content
            try:
                # Validate the response content with Pydantic
                action_response = NextAction.model_validate_json(response_content)

                # Return the validated JSON response
                return action_response.model_dump_json()

            except UnicodeDecodeError as e:
                logger.error(f"UTF-8 decoding error: {str(e)}")
                return '{"action": "", "observation": "UTF-8 decoding error"}'

            except ValidationError as e:
                # Handle validation errors with Pydantic
                logger.error(f"Validation error: {e.json()}")
                return '{"action": "", "observation": "Validation error"}'

        except Exception as e:
            # Log the error
            logger.error(f"Error fetching next action: {str(e)}")
            return '{"action": "", "observation": "Error fetching next action"}'


