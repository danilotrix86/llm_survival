from abc import ABC, abstractmethod
import os
from dotenv import load_dotenv
import logging

class AIWrapper(ABC):
    """
    Abstract base class for AI model wrappers. Defines common methods and attributes for all AI models.

    Attributes:
    -----------
    model : str
        The name or identifier of the AI model to use.
    api_key : str
        The API key for authenticating with the AI service.
    messages : list
        A list to store messages in the format {"role": role, "content": content}.
    """
    def __init__(self, model, env_var_name):
        """
        Initializes a new instance of the AIWrapper class.

        Parameters:
        -----------
        model : str
            The name or identifier of the AI model to use.
        env_var_name : str
            The name of the environment variable that contains the API key.
        """
        load_dotenv()  # Load environment variables from a .env file.
        self.api_key = os.getenv(env_var_name)  # Retrieve the API key from the environment variable.
        self.model = model  # Set the AI model name.
        self.messages = []  # Initialize an empty list for storing messages.
        self._initialize_client()  # Initialize the AI client.

    @abstractmethod
    def _initialize_client(self):
        """
        Abstract method to initialize the AI client. Must be implemented by subclasses.
        """
        pass

    def add_message(self, role, content):
        """
        Adds a message to the messages list.

        Parameters:
        -----------
        role : str
            The role of the message sender (e.g., "user" or "system").
        content : str
            The content of the message.
        """
        self.messages.append({"role": role, "content": content})

    def remove_message(self, index):
        """
        Removes a message from the messages list at the specified index.

        Parameters:
        -----------
        index : int
            The index of the message to remove.

        Raises:
        -------
        IndexError
            If the index is out of range.
        """
        if 0 <= index < len(self.messages):
            del self.messages[index]
        else:
            raise IndexError("Message index out of range")

    def clear_messages(self):
        """
        Clears all messages from the messages list.
        """
        self.messages = []

    def get_messages(self):
        """
        Returns the list of messages.

        Returns:
        --------
        list
            The current list of messages.
        """
        return self.messages

    def completion(self, response_format="text", **kwargs):
        """
        Requests a completion from the AI model.

        Parameters:
        -----------
        response_format : str, optional
            The format of the response ("text" or "json"). Default is "text".
        **kwargs
            Additional parameters to pass to the completion request.

        Returns:
        --------
        object
            The completion response from the AI model.
        """
        api_params = {
            "model": self.model,  # Include the model name in the request parameters.
            "messages": self.messages,  # Include the messages in the request parameters.
            **kwargs
        }

        # Uncomment the following line to enable logging of API call parameters.
        # logging.info(f"Calling completion with params: {api_params}")
        
        if response_format == "json":
            api_params["response_format"] = {"type": "json_object"}  # Set the response format to JSON if specified.

        return self._create_completion(api_params)  # Call the method to create the completion.

    @abstractmethod
    def _create_completion(self, api_params):
        """
        Abstract method to create a completion. Must be implemented by subclasses.

        Parameters:
        -----------
        api_params : dict
            The parameters for the completion request.

        Returns:
        --------
        object
            The completion response from the AI model.
        """
        pass

class OpenAIWrapper(AIWrapper):
    """
    Wrapper class for the OpenAI API.
    """
    def __init__(self, model):
        """
        Initializes a new instance of the OpenAIWrapper class.

        Parameters:
        -----------
        model : str
            The name or identifier of the OpenAI model to use.
        """
        super().__init__(model, 'OPENAI_API_KEY')

    def _initialize_client(self):
        """
        Initializes the OpenAI client.
        """
        import openai  # Import the OpenAI library.
        openai.api_key = self.api_key  # Set the API key for the OpenAI client.
        try:
            self.client = openai.OpenAI(api_key=self.api_key)  # Initialize the OpenAI client.
            logging.info("OpenAI client initialized successfully")  # Log successful initialization.
        except Exception as e:
            logging.error(f"Error initializing OpenAI client: {e}")  # Log any errors during initialization.

    def _create_completion(self, api_params):
        """
        Creates a completion using the OpenAI API.

        Parameters:
        -----------
        api_params : dict
            The parameters for the completion request.

        Returns:
        --------
        object
            The completion response from the OpenAI API.
        """
        return self.client.chat.completions.create(**api_params)

class GroqWrapper(AIWrapper):
    """
    Wrapper class for the Groq API.
    """
    def __init__(self, model):
        """
        Initializes a new instance of the GroqWrapper class.

        Parameters:
        -----------
        model : str
            The name or identifier of the Groq model to use.
        """
        super().__init__(model, 'GROQ_API_KEY')

    def _initialize_client(self):
        """
        Initializes the Groq client.
        """
        from groq import Groq  # Import the Groq library.
        try:
            self.client = Groq(api_key=self.api_key)  # Initialize the Groq client.
            logging.info("Groq client initialized successfully")  # Log successful initialization.
        except Exception as e:
            logging.error(f"Error initializing Groq client: {e}")  # Log any errors during initialization.

    def _create_completion(self, api_params):
        """
        Creates a completion using the Groq API.

        Parameters:
        -----------
        api_params : dict
            The parameters for the completion request.

        Returns:
        --------
        object
            The completion response from the Groq API.
        """
        return self.client.chat.completions.create(**api_params)
