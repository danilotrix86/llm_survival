from pydantic import BaseModel, Field
import json
import json
from langchain_core.agents import AgentActionMessageLog, AgentFinish


class Inventory(BaseModel):
    axe: int
    fibers: int
    stone: int
    wood: int
    stick: int

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


class NextAction(BaseModel):
    action: str 
    observation: str
    
