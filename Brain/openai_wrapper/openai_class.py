import openai
from dotenv import load_dotenv
import os

class OpenAIWrapper:
    def __init__(self, model):
        """
        Initialize the OpenAIWrapper instance.

        Args:
            api_key (str): The API key for OpenAI.
            model (str): The model name to use for chat completions.
        """
        # Load environment variables from .env file
        load_dotenv()

        self.api_key = os.getenv('OPENAI_API_KEY')
        self.model = model
        self.messages = []
        openai.api_key = self.api_key
        self.client = openai.OpenAI(api_key=self.api_key)  # Initialize OpenAI client

    def add_message(self, role, content):
        """
        Add a message to the conversation.

        Args:
            role (str): The role of the message, either "system", "user", or "assistant".
            content (str): The content of the message.
        """
        self.messages.append({"role": role, "content": content})

    def remove_message(self, index):
        """
        Remove a message from the conversation by index.

        Args:
            index (int): The index of the message to remove.

        Raises:
            IndexError: If the index is out of range.
        """
        if 0 <= index < len(self.messages):
            del self.messages[index]
        else:
            raise IndexError("Message index out of range")
        
    def clear_messages(self):
        """Clear all messages from the conversation."""
        self.messages = []

    def get_messages(self):
        """Get the conversation messages."""
        return self.messages

    def completion(self, response_format="text", **kwargs):
        """
        Create a chat completion using the conversation messages.

        Args:
            response_format (str): The format of the response, either "text" or "json". Defaults to "text".
            **kwargs: Additional keyword arguments to pass to the API.

        Returns:
            dict: The response from the OpenAI API.
        """
        # Prepare the API call parameters
        api_params = {
            "model": self.model,
            "messages": self.messages,
            **kwargs
        }
        
        # Set the response format if JSON is requested
        if response_format == "json":
            api_params["response_format"] = {"type": "json_object"}

        # Make the API call to get the completion
        response = self.client.chat.completions.create(**api_params)
        return response