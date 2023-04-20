using System.Drawing;

namespace Eksamen.Shared
{
    public class Player
    {
        public string Name { get; set; }

        public List<Tile> PlacedTiles { get; set; }

        public int selectedAreas = 0;

        public Player(string name)
        {
            Name = name;
            PlacedTiles = new List<Tile>();
        }

    }
}
