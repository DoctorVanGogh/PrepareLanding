﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.GameData
{
    public class GodModeData : INotifyPropertyChanged
    {
        private const float FloatEpsilon = 1e-03f;

        private float _averageTemperature;

        private BiomeDef _biome;

        private float _elevation;

        private Hilliness _hilliness;

        private float _rainfall;

        private int _selectedTileId = -1;

        private readonly DefData _defData;

        public GodModeData(DefData defData)
        {
            _defData = defData;

            // get alerted when RimWorld has loaded its definition (Defs) files
            _defData.DefParsed += InitDefs;
        }

        private void InitDefs()
        {
            // initialize roads
            foreach (var roadDef in _defData.RoadDefs)
                SelectedRoadDefs.Add(roadDef, false);

            // initialize rivers
            foreach (var riverDef in _defData.RiverDefs)
                SelectedRiverDefs.Add(riverDef, false);

            // initialize stones
            foreach (var stoneDef in _defData.StoneDefs)
            {
                SelectedStoneDefs.Add(stoneDef, false);
                OrderedStoneDefs.Add(stoneDef);
            }

            OrderedStoneDefs.Sort((x, y) => string.Compare(x.LabelCap, y.LabelCap, StringComparison.Ordinal));
        }

        public void InitFromTileId(int tileId)
        {
            if (tileId < 0)
                return;

            _selectedTileId = tileId;

            var tile = Find.World.grid[tileId];

            _biome = tile.biome;
            _elevation = tile.elevation;
            _hilliness = tile.hilliness;
            _averageTemperature = tile.temperature;
            _rainfall = tile.rainfall;

            ResetSelectedRoadDefs();
            if(tile.VisibleRoads != null)
            { 
                foreach (var visibleRoad in tile.VisibleRoads)
                {
                    var roadDef = visibleRoad.road;
                    SelectedRoadDefs[roadDef] = true;
                }
            }

            ResetSelectedRiverDefs();
            if(tile.VisibleRivers != null)
            { 
                foreach (var visibleRiver in tile.VisibleRivers)
                {
                    var riverDef = visibleRiver.river;
                    SelectedRiverDefs[riverDef] = true;
                }
            }

            ResetSelectedStoneDefs();
            foreach (var stoneDef in Find.World.NaturalRockTypesIn(tileId))
            {              
                if (SelectedStoneDefs.ContainsKey(stoneDef))
                {
                    SelectedStoneDefs[stoneDef] = true;
                }
            }
        }

        public bool SetupTile()
        {
            if (SelectedTileId < 0)
                return false;

            var tile = Find.World.grid[SelectedTileId];

            /*
             * Validity checks
             */

            var countTrue = SelectedStoneDefs.Values.Count(stoneDefValue => stoneDefValue);
            if (countTrue < 2 || countTrue > 3)
            {
                Messages.Message($"Number of stones must be in the range: [{2}, {3}]", MessageSound.RejectInput);
                return false;
            }

            if (HasSelectedRoadDefs && !Biome.allowRoads)
            {
                Messages.Message($"The selected biome ({Biome.LabelCap}) doesn't support roads.", MessageSound.RejectInput);
                return false;
            }

            if (HasSelectedRiverDefs && !Biome.allowRivers)
            {
                Messages.Message($"The selected biome ({Biome.LabelCap}) doesn't support rivers.", MessageSound.RejectInput);
                return false;
            }

            /*
             * Setup tile
             */

            if (Biome != null)
                tile.biome = Biome;

            tile.temperature = AverageTemperature;

            if (Hilliness != Hilliness.Undefined)
                tile.hilliness = Hilliness;

            tile.elevation = Elevation;

            tile.rainfall = Rainfall;

            SetupTileRoads(tile);

            return true;
        }

        private void SetupTileRoads(Tile tile)
        {
            if (!tile.biome.allowRoads)
                return;

            if (tile.VisibleRoads != null)
                tile.VisibleRoads.Clear();
            else
            {
                tile.roads = new List<Tile.RoadLink>();
            }

            foreach (var keyValueRoadDef in SelectedRoadDefs)
            {
                if (!keyValueRoadDef.Value)
                    continue;

                var roadLink = new Tile.RoadLink
                {
                    road = keyValueRoadDef.Key,
                    neighbor = -1 // BUG: this doesn't work, you need to have a valid neighbor; see RimWorld.Planet.WorldGenStep_Roads.GenerateRoadNetwork()
                };

                tile.VisibleRoads?.Add(roadLink);
            }
        }

        private void SetupTileRivers(Tile tile)
        {
            if (!tile.biome.allowRivers)
                return;

            tile.VisibleRivers.Clear();
            foreach (var keyValueRiverDef in SelectedRiverDefs)
            {
                if (!keyValueRiverDef.Value)
                    continue;

                var riverLink = new Tile.RiverLink
                {
                    river = keyValueRiverDef.Key,
                    neighbor = -1 // BUG: this doesn't work, you need to have a valid neighbor; see RimWorld.Planet.WorldGenStep_Rivers.GenerateRivers()
                };

                tile.VisibleRivers.Add(riverLink);
            }
        }

        public void ResetSelectedRoadDefs()
        {
            foreach (var roadDef in SelectedRoadDefs.Keys.ToList())
            {
                SelectedRoadDefs[roadDef] = false;
            }
        }

        public void ResetSelectedRiverDefs()
        {
            foreach (var riverDef in SelectedRiverDefs.Keys.ToList())
            {
                SelectedRiverDefs[riverDef] = false;
            }
        }

        public void ResetSelectedStoneDefs()
        {
            foreach (var stoneDef in SelectedStoneDefs.Keys.ToList())
            {
                SelectedStoneDefs[stoneDef] = false;
            }
            OrderedStoneDefs.Sort((x, y) => string.Compare(x.LabelCap, y.LabelCap, StringComparison.Ordinal));
        }

        /// <summary>
        ///     The id of the tile to modify.
        /// </summary>
        public int SelectedTileId
        {
            get { return _selectedTileId; }
            set
            {
                if (value == _selectedTileId)
                    return;

                _selectedTileId = value;
                OnPropertyChanged(nameof(SelectedTileId));
            }
        }

        /// <summary>
        ///     The new biome to set in the tile.
        /// </summary>
        public BiomeDef Biome
        {
            get { return _biome; }
            set
            {
                if (value == _biome)
                    return;

                _biome = value;
                OnPropertyChanged(nameof(Biome));
            }
        }

        /// <summary>
        ///     Average temperature (in degrees Celsius) to set in the tile.
        /// </summary>
        public float AverageTemperature
        {
            get { return _averageTemperature; }
            set
            {
                if (Math.Abs(value - _averageTemperature) < FloatEpsilon)
                    return;

                _averageTemperature = value;
                OnPropertyChanged(nameof(AverageTemperature));
            }
        }

        /// <summary>
        ///     New Rainfall (millimeters) to set in the tile.
        /// </summary>
        public float Rainfall
        {
            get { return _rainfall; }
            set
            {
                if (Math.Abs(value - _rainfall) < FloatEpsilon)
                    return;

                _rainfall = value;
                OnPropertyChanged(nameof(Rainfall));
            }
        }

        /// <summary>
        ///     New elevation (meters) to set in the tile.
        /// </summary>
        public float Elevation
        {
            get { return _elevation; }
            set
            {
                if (Math.Abs(value - _elevation) < FloatEpsilon)
                    return;

                _elevation = value;
                OnPropertyChanged(nameof(Elevation));
            }
        }


        /// <summary>
        ///     New hilliness to set in the tile.
        /// </summary>
        public Hilliness Hilliness
        {
            get { return _hilliness; }
            set
            {
                if (value == _hilliness)
                    return;

                _hilliness = value;
                OnPropertyChanged(nameof(Hilliness));
            }
        }

        public bool HasSelectedRoadDefs => SelectedRoadDefs.Any(r => r.Value);

        public bool HasSelectedRiverDefs => SelectedRiverDefs.Any(r => r.Value);

        public Dictionary<RoadDef, bool> SelectedRoadDefs { get; } = new Dictionary<RoadDef, bool>();
        
        public Dictionary<RiverDef, bool> SelectedRiverDefs { get; } = new Dictionary<RiverDef, bool>();

        public Dictionary<ThingDef, bool> SelectedStoneDefs { get; } = new Dictionary<ThingDef, bool>();

        public List<ThingDef> OrderedStoneDefs { get; } = new List<ThingDef>();


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}