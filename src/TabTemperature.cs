﻿using System.Collections.Generic;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using UnityEngine;
using Verse;
using Widgets = PrepareLanding.Core.Gui.Widgets;

namespace PrepareLanding
{
    public class TabTemperature : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;

        public TabTemperature(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>
        ///     A unique identifier for the Tab.
        /// </summary>
        public override string Id => Name;

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "Temperature";

        /// <summary>
        ///     Draw the actual content of this window.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> inside which to draw.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawTemperaturesSelection();
            DrawGrowingPeriodSelection();
            NewColumn();
            DrawRainfallSelection();

            // "Animals Can Graze Now" relies on game ticks as VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt calls
            //   GenTemperature.GetTemperatureAtTile which calls GenTemperature.GetTemperatureFromSeasonAtTile
            //  This last function takes GenTicks.TicksAbs as argument but the results are not consistent between calls...
            // All in all: better not calling it during the "select landing site" page.
            if (GenScene.InPlayScene)
                DrawAnimalsCanGrazeNowSelection();
            End();
        }

        protected virtual void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader("Animals", backgroundColor: ColorFromFilterSubjectThingDef("Animals Can Graze Now"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenAnimalsCanGrazeNowState;
            Widgets.CheckBoxLabeledMulti(rect, "Animals Can Graze Now:", ref tmpCheckState);

            _gameData.UserData.ChosenAnimalsCanGrazeNowState = tmpCheckState;
        }

        protected void DrawGrowingPeriodSelection()
        {
            const string label = "Growing Period";
            DrawEntryHeader($"{label} (days)", backgroundColor: ColorFromFilterSubjectThingDef("Growing Periods"));

            var boundField = _gameData.UserData.GrowingPeriod;

            var tmpCheckedOn = boundField.Use;

            ListingStandard.Gap();
            ListingStandard.CheckboxLabeled(label, ref tmpCheckedOn, $"Use Min/Max {label}");
            boundField.Use = tmpCheckedOn;

            // MIN

            if (ListingStandard.ButtonText($"Min {label}"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var growingTwelfth in boundField.Options)
                {
                    var menuOption = new FloatMenuOption(growingTwelfth.GrowingDaysString(),
                        delegate { boundField.Min = growingTwelfth; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, $"Select {label}");
                Find.WindowStack.Add(floatMenu);
            }

            ListingStandard.LabelDouble($"Min. {label}:", boundField.Min.GrowingDaysString());

            // MAX

            if (ListingStandard.ButtonText($"Max {label}"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var growingTwelfth in boundField.Options)
                {
                    var menuOption = new FloatMenuOption(growingTwelfth.GrowingDaysString(),
                        delegate { boundField.Max = growingTwelfth; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, $"Select {label}");
                Find.WindowStack.Add(floatMenu);
            }

            ListingStandard.LabelDouble($"Max. {label}:", boundField.Max.GrowingDaysString());
        }

        protected virtual void DrawRainfallSelection()
        {
            DrawEntryHeader("Rain Fall (mm)", backgroundColor: ColorFromFilterSubjectThingDef("Rain Falls"));

            DrawUsableMinMaxNumericField(_gameData.UserData.RainFall, "Rain Fall");
        }

        protected void DrawTemperaturesSelection()
        {
            DrawEntryHeader("Temperatures (Celsius)",
                backgroundColor: ColorFromFilterSubjectThingDef("Average Temperatures"));

            DrawUsableMinMaxNumericField(_gameData.UserData.AverageTemperature, "Average Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_gameData.UserData.WinterTemperature, "Winter Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_gameData.UserData.SummerTemperature, "Summer Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
        }
    }
}