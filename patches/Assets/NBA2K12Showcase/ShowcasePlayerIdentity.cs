using UnityEngine;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Presentation-only roster identity. BasketballTeamMember remains the authority for team/player index.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShowcasePlayerIdentity : MonoBehaviour
    {
        [SerializeField] private string playerName = "PLAYER";
        [SerializeField] private string position = "G";
        [SerializeField, Range(0, 99)] private int jerseyNumber = 0;
        [SerializeField] private string teamName = "TEAM";

        public string PlayerName => playerName;
        public string Position => position;
        public int JerseyNumber => jerseyNumber;
        public string TeamName => teamName;

        public void Configure(string name, string positionValue, int number, string team)
        {
            playerName = string.IsNullOrWhiteSpace(name) ? "PLAYER" : name.Trim();
            position = string.IsNullOrWhiteSpace(positionValue) ? "G" : positionValue.Trim();
            jerseyNumber = Mathf.Clamp(number, 0, 99);
            teamName = string.IsNullOrWhiteSpace(team) ? "TEAM" : team.Trim();
        }
    }
}
