using Microsoft.AspNetCore.Components;

namespace Eksamen.Shared
{
    public static class Store
    {
        public static List<TileType> TileTypes { get; set; }
        public static List<Tile> Deck { get; set; }
        public static Tile[,] Board { get; set; }
        public static List<Player> Players { get; set; }

        public static class GameSettings
        {
            public static int BoardSize = 15;
            public static int PlayerCount = 3;
            public static int MaxAreasPerPlayer = 10; 
        }   
    }
}
