import sys
import uvicorn
import os
import logging

# Add the app directory to the system path
sys.path.append('./app')

from app.main import app

if __name__ == "__main__":
    logging.INFO(os.getenv("PYTHONIOENCODING"))
    uvicorn.run("app.main:app", host="127.0.0.1", port=8000, reload=True)