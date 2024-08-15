# AI CASTAWAY - Can a LLM survive on a remote island?
[![AI Castaway YouTube Cover](https://www.danilovaccalluzzo.it/llm_survival/ai_castaway_youtube_cover.jpg)](https://www.youtube.com/watch?v=eHU7Kmio8Mw)
**[Watch the video on YouTube](https://www.youtube.com/watch?v=eHU7Kmio8Mw)**

This project showcases an advanced AI-driven survival game where the AI agent is powered by large language models (LLMs). The system allows the AI to navigate complex survival scenarios dynamically, adapting its behavior based on its environment and experiences.

## Overview

This project is part of a master's degree thesis in Artificial Intelligence, demonstrating an AI-driven survival game called **AI Castaway**. In this game, the AI agent is placed on a remote island and must autonomously manage resources, track vital statistics, and make strategic decisions to survive. Powered by large language models (LLMs), the AI dynamically adapts its actions based on the environment and past experiences. The result is a unique survival simulation where the AI independently gathers resources, crafts tools, and builds structures, showcasing the potential of advanced AI architectures in game environments.





## Getting Started

To start using the project:

### Prerequisites

- Python 3.x
- [pip](https://pip.pypa.io/en/stable/installation/) for package management

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/danilotrix86/llm_survival.git
   cd llm_survival
   ```

2. Set up a virtual environment (optional):
   ```bash
   python -m venv venv
   # Activate the virtual environment
   # On Windows
   venv\Scripts\activate
   # On macOS/Linux
   source venv/bin/activate
   ```

3. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

## Configuration

To configure the project, modify the `config.py` file. Below are the key settings you need to adjust for the AI's behavior and decision-making process.

### LLM Engines

The AI's behavior is powered by different large language models (LLMs). The choice of LLM engine impacts both performance and cost:

- **OpenAI**: Uses OpenAI’s models. To use this, set `LLM_ENGINE` to `"openai"` and set `GPT_ENGINE` to one of the following models:
  - `"gpt-4o"`
  - `"gpt-4o-mini"`
  - `"gpt-3.5-turbo"`

- **Groq API**: Offers cost-effective models through Groq. To use these models, set `LLM_ENGINE` to one of the following models:
  - `"llama3-8b-8192"`
  - `"llama3-70b-8192"`
  - `"llama-3.1-70b-versatile"`
  - `"mixtral-8x7b-32768"`
  - `"gemma-7b-it"`
  - `"gemma2-9b-it"`

`GPT_ENGINE` is not important with these settings.
### Approaches

There are two possible decision-making approaches for the AI:

- **`ZEROSHOT`**: The Zero-Shot approach is designed for simplicity and efficiency. In this method, a single call is made to the large language model (LLM) with all the relevant game data (including the AI’s current state, environment, and memory). The LLM processes this information in one shot and generates the next action for the AI.
  
- **`AGENTIC`**: The Agentic approach is more sophisticated, allowing the AI to make context-sensitive decisions by querying specific pieces of information as needed. Instead of providing the LLM with all the data at once, the AI agent selectively retrieves relevant information (e.g., current vital stats, environmental changes) before deciding on an action. This approach leverages frameworks like LangChain to dynamically access different memory components.

### Configuration File Example

Here’s an example of the key configuration settings in `config.py`:

```python
LOGS_SIZE = 8  # Number of logs to maintain

APPROACH = "ZEROSHOT"  # Choose between "ZEROSHOT" or "AGENTIC"

LLM_ENGINE = "openai"  # Set to "openai" or one of the following: "llama3-8b-8192", "llama3-70b-8192", "llama-3.1-70b-versatile", "llama-3.1-405b-reasoning" ONLY FOR PAYING MEMBERS,  "mixtral-8x7b-32768", "gemma-7b-it", "gemma2-9b-it"

# If LLM_ENGINE is "openai", choose the GPT engine
GPT_ENGINE = "gpt-4o"  # Choose between "gpt-4o", "gpt-4o-mini", or "gpt-3.5-turbo"

LLM_TEMPERATURE = 0.5  # Controls randomness of responses (0 = more deterministic, 1 = more random)
```
### Environment Variables

Create a `.env` file in the root directory to configure API keys for the LLM engines. The file should include:

For OpenAI Engine:

```plaintext
OPENAI_API_KEY=your_openai_api_key_here
```

For Groq API Engine:

```plaintext
GROQ_API_KEY=your_groq_api_key_here
```

Replace `your_openai_api_key_here` and `your_groq_api_key_here` with your actual API keys.

### Running the Application

1. Start the server using `uvicorn`:
   ```bash
   python.exe .\run.py
   ```
   The API will be accessible at `http://127.0.0.1:8000`.

2. After the server is running, you can launch the game:

   - Navigate to the `game` folder.
   - Inside, you will find subfolders for Windows, Linux, and Mac. Choose the appropriate version for your operating system and launch the corresponding executable.

**Note:** The server must be started and running before launching the game executable. Ensure that the API is live at `http://127.0.0.1:8000` for the game to function properly.

## API Endpoints

- **`GET /messages/`**: Fetches messages from the game settings.
- **`GET /xp/`**: Fetches experience points (xp) data.
- **`POST /next_action/`**: Determines the next action based on the received request. Supports different approaches (`ZEROSHOT` or `AGENTIC`).
- **`POST /start_new_game/`**: Starts a new game session, resetting logs and player information.

## Error Handling

The API provides detailed error messages and status codes to help with debugging and usage.

## Logging

The application uses Python's `logging` module for tracking important events and errors. Logs are displayed in the console and can be further configured in `logging.basicConfig`.

## Source Code

If you want to download the gaame source code, here's the GitHub repository:  
[https://github.com/danilotrix86/llm_survival_source](https://github.com/danilotrix86/llm_survival_source)

## Credits

This project is part of a master's degree thesis in Artificial Intelligence. The project was developed by Danilo Vaccalluzzo.

