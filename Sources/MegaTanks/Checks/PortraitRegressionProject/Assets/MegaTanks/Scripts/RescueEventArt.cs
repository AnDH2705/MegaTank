using UnityEngine;
namespace MegaTanks
{
    public sealed class RescueEventArt : ScriptableObject
    {
        public Sprite cage,crewPortrait;
        public static Sprite Cage=>Resources.Load<RescueEventArt>("RescueEventArt")?.cage;
        public static Sprite CrewPortrait=>Resources.Load<RescueEventArt>("RescueEventArt")?.crewPortrait;
    }
}
