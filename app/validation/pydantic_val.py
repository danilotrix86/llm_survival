from pydantic import BaseModel

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
