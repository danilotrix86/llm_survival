import sys
import uvicorn
import os
import logging

# Add the app directory to the system path
sys.path.append('./app')

from app.main import app

# Configure logging
logging.basicConfig(level=logging.INFO)

if __name__ == "__main__":
    # Log the PYTHONIOENCODING environment variable
    logging.info(f"PYTHONIOENCODING: {os.getenv('PYTHONIOENCODING')}")
    uvicorn.run("app.main:app", host="127.0.0.1", port=8000, reload=True)
