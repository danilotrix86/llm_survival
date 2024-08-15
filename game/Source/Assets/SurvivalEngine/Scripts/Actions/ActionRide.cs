using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Action to attack a destructible (if the destructible cant be attack automatically)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Ride", order = 50)]
    public class ActionRide : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            AnimalRide ride = select.GetComponent<AnimalRide>();
            if (ride != null)
            {
                character.Riding.RideAnimal(ride);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            AnimalRide ride = select.GetComponent<AnimalRide>();
            return ride != null && !ride.IsDead() && character.Riding != null;
        }
    }

}