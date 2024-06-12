# validation/pydantic_val.py

from pydantic import BaseModel
from typing import Dict

class Inventory(BaseModel):
    food: int
    water: int
    shelter: int
    fish: int
    berry: int
    stick: int
    wood: int
    stone: int
    fiber: int
    ax: int
    firecamp: int
    raft: int

class PlayerInfo(BaseModel):
    health: str
    hunger: str
    thirst: str
    energy: str  

class ActionRequest(BaseModel):
    action: str
    status: str
    message: str
    inventory: Inventory
    player_info: PlayerInfo
