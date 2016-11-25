using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Constants;
using SharpDX;
using Color = System.Drawing.Color;
using System.Collections.Generic;

namespace FishFishFish.ManagerMenu
{
    class ManagerMenu
    {
        public static Menu Menu { get; set; }
        public static Menu comboMenu, harassMenu, laneclearMenu, miscMenu, drawMenu;
        public static void CreateMenu()
        {
            Menu = MainMenu.AddMenu("Fizz", "Fish Fish Fish");

            comboMenu = Menu.AddSubMenu("Combo");
            comboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            comboMenu.Add("UseWCombo", new CheckBox("Use W"));
            comboMenu.Add("UseECombo", new CheckBox("Use E"));
            comboMenu.Add("UseRCombo", new CheckBox("Use R"));
            comboMenu.Add("QRCombo", new CheckBox("Use QR Combo"));
            comboMenu.Add("UseREGapclose", new CheckBox("Use R, then E for gapclose if killable"));

            harassMenu = Menu.AddSubMenu("Harass");
            harassMenu.Add("UseQMixed", new CheckBox("UseQ"));
            harassMenu.Add("UseWMixed", new CheckBox("UseW"));
            harassMenu.Add("UseEMixed", new CheckBox("UseE"));
            StringList(harassMenu, "UseEHarassMode", "E Mode: ", new[] { "Back to Position", "On Enemy" }, 1);

            laneclearMenu = Menu.AddSubMenu("Lane Clear");
            laneclearMenu.Add("UseQM", new CheckBox("UseQ"));
            laneclearMenu.Add("UseW", new CheckBox("UseW"));
            laneclearMenu.Add("UseE", new CheckBox("UseE"));
            laneclearMenu.Add("laneclear", new Slider("Min. Mana for Laneclear Spells %", 70, 0, 100));

            miscMenu = Menu.AddSubMenu("Misc");
            StringList(miscMenu, "UseWWhen", "Use W", new[] { "Before Q", "After Q" }, 0);
            miscMenu.Add("UseETower", new CheckBox("Dodge tower shots with E"));

            drawMenu = Menu.AddSubMenu("Drawing");
            drawMenu.Add("DrawQ", new CheckBox("Draw Q"));
            drawMenu.Add("DrawE", new CheckBox("Draw E"));
            drawMenu.Add("DrawR", new CheckBox("Draw R"));
            drawMenu.Add("DrawRPred", new CheckBox("Draw R Prediction"));
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ":  " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }
    }
}
