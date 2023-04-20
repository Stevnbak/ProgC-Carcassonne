using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Eksamen.Shared
{
    public class Tile
    {
        public int TopSide;
        public TileType Type;
        public Player? Player { get; set; }
        public KeyValuePair<int,char>? SelectedArea { get; set; }
        public string? OverlayPath { get; set; }

        public Tile(TileType type, int topSide)
        {
            this.Type = type;
            this.TopSide = topSide;
        }

        public bool IsConnected(char sideType)
        {
            //Multiple connected?
            int sideCount = Type.sides.ToList().FindAll((a) => a == sideType).Count;
            if (sideCount > 1 && ((sideType == 't' && (Type.image != "tile14.png" && Type.image != "tile15.png")) || (sideType == 'g' && Type.image != "tile03.png") || (sideType == 'r' && sideCount == 2)))
            {
                //Area is connected within tile
                return true;
            }
            else
            {
                //Solo or split
                return false;
            }
        }

        public void SetArea(char area, int side)
        {
            SelectedArea = new KeyValuePair<int, char>(side, area);
            //Set overlay
            if (side == 4) OverlayPath = "center.png";
            else
            {
                int sideLocation = side - TopSide;
                if (sideLocation < 0) sideLocation += 4;
                OverlayPath = "side" + sideLocation + ".png";
            }
            
        }

        public List<KeyValuePair<Tile, int>>[]? GetRegions()
        {
            //Find location on board
            int[]? location = BoardLocationOfTile(this);
            if (location == null) return null;
            //Find regions
            List<KeyValuePair<Tile, int>>?[] regions = new List<KeyValuePair<Tile, int>>[Type.sides.Length];
            for (int s = 0; s < this.Type.sides.Length; s++) {
                //Add current tile to region
                if (regions[s] == null) regions[s] = new List<KeyValuePair<Tile, int>>();
                regions[s]?.Add(new KeyValuePair<Tile, int>(this, s));

                //Find first adjacent
                KeyValuePair<Tile, int>? startTile = FindSameAdjecant(this, location, s);
                if(!startTile.HasValue) continue;

                List<KeyValuePair<Tile, int>> tilesToCheck = new List<KeyValuePair<Tile, int>>() { startTile.Value };

                while (tilesToCheck.Count > 0) {
                    Tile tile = tilesToCheck.First().Key;
                    int sideIndex = tilesToCheck.First().Value;

                    //Remove from check list
                    tilesToCheck.RemoveAt(0);

                    //Side
                    char side = tile.Type.sides[sideIndex];
                    if (side != this.Type.sides[s]) continue;

                    //Multiple connected?
                    if(tile.IsConnected(side))
                    {
                        for(int i = 0; i < tile.Type.sides.Length; i++)
                        {
                            char side2 = tile.Type.sides[i];
                            if (side2 != side) continue;
                            //Add to region
                            regions[s]?.Add(new KeyValuePair<Tile, int>(tile, i));
                            //Add to enumerable
                            int[]? otherLocation = BoardLocationOfTile(tile);
                            if (otherLocation == null) continue;
                            KeyValuePair<Tile, int>? otherTile = FindSameAdjecant(tile, otherLocation, i);
                            if (otherTile.HasValue == false) continue;
                            if (tilesToCheck.Contains(otherTile.Value) || regions[s].Contains(otherTile.Value)) continue;
                            tilesToCheck.Add(otherTile.Value);
                        }
                    } else
                    {
                        regions[s]?.Add(new KeyValuePair<Tile, int>(tile, sideIndex));
                    }
                }
            }

            KeyValuePair<Tile, int>? FindSameAdjecant(Tile tile, int[] boardLocation, int startSide)
            {
                //Location
                int x = boardLocation[0];
                int y = boardLocation[1];
                
                //Sides & new location
                int sideLocation = startSide - tile.TopSide;
                if (sideLocation < 0) sideLocation += 4;
                int newX = sideLocation == 0 || sideLocation == 2 ? x : sideLocation == 1 ? x + 1 : x - 1;
                int newY = sideLocation == 1 || sideLocation == 3 ? y : sideLocation == 0 ? y - 1 : y + 1;

                //Is same?
                if (newX < 0 || newX >= Store.GameSettings.BoardSize) return null;
                if (newY < 0 || newY >= Store.GameSettings.BoardSize) return null;
                if (Store.Board[newX, newY] != null)
                {
                    Tile otherTile = Store.Board[newX, newY];
                    int relevantSideOther = newX < x ? 1 : newX > x ? 3 : newY < y ? 2 : 0;
                    relevantSideOther += otherTile.TopSide;
                    if (relevantSideOther >= 4) relevantSideOther -= 4;

                    if (otherTile.Type.sides[relevantSideOther] == tile.Type.sides[startSide])
                    {
                        return new KeyValuePair<Tile, int>(otherTile, relevantSideOther);
                    }
                }
                return null;
            }

            //Combine regions if connected
            for(int r = 0; r < regions.Length; r++)
            {
                if(IsConnected(Type.sides[r]))
                {
                    for (int s = 0; s < Type.sides.Length; s++)
                    {
                        if (Type.sides[s] == Type.sides[r] && s != r && regions[s] != null)
                        {
                            if (regions[r] == null) continue;
                            regions[r] = regions[r].Concat(regions[s] ?? new List<KeyValuePair<Tile, int>>()).ToList();
                            regions[s] = null;
                        }
                    }
                }
            }

            return regions;
        }

        public static Player? GetPlayerOwningRegion(List<KeyValuePair<Tile, int>> region)
        {
            //Rest
            if (region == null) return null;
            Dictionary<Player, int> amount = new Dictionary<Player, int>();
            if (region.Count == 0) return null;
            char side = region.ElementAt(0).Key.Type.sides[region.ElementAt(0).Value];
            region.ForEach((valuePair) =>
            {
                if (!valuePair.Key.SelectedArea.HasValue) return;
                if (valuePair.Key.SelectedArea.Value.Value != side) return;
                Player? player = null;

                //Selected through connection?
                if(valuePair.Key.IsConnected(side))
                {
                    //Area is connected
                    if (valuePair.Key.SelectedArea.Value.Value == side)
                    {
                        player = valuePair.Key.Player;
                    }
                } else
                {
                    //Area has been selected on this tile and side
                    if (valuePair.Key.SelectedArea.Value.Key == valuePair.Value)
                    {
                        //Correct side has been selected on this tile
                        player = valuePair.Key.Player;
                    }
                }
                //Add to player amount
                if (player == null) return;
                if (amount.ContainsKey(player) == false)
                {
                    amount.Add(player, 1);
                }
                else
                {
                    amount[player] += 1;
                }
            });

            //Find player with most amount
            if (amount.Count == 0) return null;
            return amount.OrderBy((a) => a.Value).First().Key;
        }

        public static int[]? BoardLocationOfTile(Tile tile)
        {
            for (int x = 0; x < Store.Board.GetLength(0); x++)
            {
                for (int y = 0; y < Store.Board.GetLength(1); y++)
                {
                    if (Store.Board[x, y] == tile)
                    {
                        return new int[2] { x, y };
                    }
                }
            }
            return null;
        }
    }

    public class TileType
    {
        private static int typeAmount = 0;
        public char[] sides; // g = grass, r = road, t = town
        public char special; // n = none, c = chirch, s = shield
        public int id, amount;
        public string image;
        public TileType(char side1, char side2, char side3, char side4, char special, int amount, string image)
        {
            this.sides = new char[4];
            this.sides[0] = side1;
            this.sides[1] = side2;
            this.sides[2] = side3;
            this.sides[3] = side4;
            this.special = special;
            this.amount = amount;
            this.image = image;
            this.id = typeAmount;
            typeAmount++;
            //Add to deck
            for (int i = 0; i < this.amount; i++)
            {
                Store.Deck.Add(new Tile(this, 0));
            }
        }
    }
}
