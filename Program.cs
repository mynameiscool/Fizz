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

namespace Fizz_Farofakids
{
    internal class Program
    {
        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static Menu Menu { get; set; }
        private static Vector3? LastHarassPos { get; set; }
        private static AIHeroClient DrawTarget { get; set; }
        private static Geometry.Polygon.Rectangle RRectangle { get; set; }

        private static Spell.Targeted Q;
        private static Spell.Active W;
        private static Spell.Skillshot E, R;


        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameOnOnGameLoad;
        }

        private static Menu comboMenu, harassMenu, miscMenu, drawMenu, laneclearMenu;

        private static void CreateMenu()
        {
            Menu = MainMenu.AddMenu("Mynameiscool", "Fizz");

            comboMenu = Menu.AddSubMenu("Combo");
            comboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            comboMenu.Add("UseWCombo", new CheckBox("Use W"));
            comboMenu.Add("UseECombo", new CheckBox("Use E"));
            comboMenu.Add("UseRCombo", new CheckBox("Use R"));
            comboMenu.Add("UseREGapclose", new CheckBox("Use R, then E for gapclose if killable"));
            comboMenu.Add("QR", new CheckBox("UUse Q then R Target"));

            harassMenu = Menu.AddSubMenu("Harass");
            harassMenu.Add("UseQMixed", new CheckBox("UseQ"));
            harassMenu.Add("UseWMixed", new CheckBox("UseW"));
            harassMenu.Add("UseEMixed", new CheckBox("UseE"));
            StringList(harassMenu, "UseEHarassMode", "E Mode: ", new[] { "Back to Position", "On Enemy" }, 1);

            miscMenu = Menu.AddSubMenu("Misc");
            StringList(miscMenu, "UseWWhen", "Use W", new[] { "Before Q", "After Q" }, 0);
            miscMenu.Add("UseETower", new CheckBox("Dodge tower shots with E"));

            laneclearMenu = Menu.AddSubMenu("Lane Clear");
            laneclearMenu.Add("UseQM", new CheckBox("UseQ"));
            laneclearMenu.Add("UseW", new CheckBox("UseW"));
            laneclearMenu.Add("UseE", new CheckBox("UseE"));
            laneclearMenu.Add("mana", new Slider("Maximum mana usage in percent)", 50));

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

        private static bool Getcheckboxvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }

        private static bool Getkeybindvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<KeyBind>().CurrentValue;
        }

        private static int Getslidervalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Fizz")
            {
                return;
            }
            Q = new Spell.Targeted(SpellSlot.Q, 550);
            W = new Spell.Active(SpellSlot.W, (uint)Player.GetAutoAttackRange());
            E = new Spell.Skillshot(SpellSlot.E, 400, SkillShotType.Circular, 250, int.MaxValue, 330);
            R = new Spell.Skillshot(SpellSlot.R, 1300, SkillShotType.Linear, 250, 1200, 80);
            E.AllowedCollisionCount = int.MaxValue;
            R.AllowedCollisionCount = 0;

            CreateMenu();

            RRectangle = new Geometry.Polygon.Rectangle(Player.Position, Player.Position, R.Width);

            Game.OnUpdate += GameOnOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Getcheckboxvalue(drawMenu, "DrawQ");
            var drawE = Getcheckboxvalue(drawMenu, "DrawE");
            var drawR = Getcheckboxvalue(drawMenu, "DrawR");
            var drawRPred = Getcheckboxvalue(drawMenu, "DrawRPred");
            var p = Player.Position;

            if (drawQ)
            {
                Drawing.DrawCircle(p, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawE)
            {
                Drawing.DrawCircle(p, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawR)
            {
                Drawing.DrawCircle(p, R.Range, R.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawRPred && R.IsReady() && DrawTarget.IsValidTarget())
            {
                RRectangle.Draw(Color.CornflowerBlue, 3);
            }
        }

        private static float DamageToUnit(AIHeroClient target)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            return (float)damage;
        }

        private static void ObjAiBaseOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is Obj_AI_Turret && args.Target.IsMe && E.IsReady() && Getcheckboxvalue(miscMenu, "UseETower"))
            {
                E.Cast(Game.CursorPos);
            }

            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "FizzPiercingStrike")
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    Core.DelayAction(() => W.Cast(), (int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) &&
                         Getslidervalue(harassMenu, "UseEHarassMode") == 0)
                {
                    Core.DelayAction(() => { JumpBack = true; }, (int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250);
                }
            }

            if (args.SData.Name == "fizzjumptwo" || args.SData.Name == "fizzjumpbuffer")
            {
                LastHarassPos = null;
                JumpBack = false;
            }
        }

        public static bool JumpBack { get; set; }

        private static void GameOnOnUpdate(EventArgs args)
        {
            DrawTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (DrawTarget.IsValidTarget())
            {
                RRectangle.Start = Player.Position.To2D();
                RRectangle.End = R.GetPrediction(DrawTarget).CastPosition.To2D();
                RRectangle.UpdatePolygon();
            }

            if (!Player.CanCast)
            {
                return;
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    DoHarass();
                    break;
                case Orbwalker.ActiveModes.Combo:
                    DoCombo();
                    break;
                case Orbwalker.ActiveModes.JungleClear:
                    Jungleclear();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    Laneclear();
                    break;
            }
        }

        public static void CastRSmart(AIHeroClient target)
        {
            var castPosition = R.GetPrediction(target).CastPosition;
            castPosition = Player.ServerPosition.Extend(castPosition, R.Range).To3D();


            R.Cast(castPosition);
        }

        public static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady())
            {
                Vector3 endPos = R.GetPrediction(target).CastPosition.Extend(Player.Position, -(600)).To3D();

                if (!target.HasBuff("summonerbarrier") || !target.HasBuff("BlackShield") || !target.HasBuff("SivirShield") || !target.HasBuff("BansheesVeil") || !target.HasBuff("ShroudofDarkness"))
                {
                    R.Cast(endPos);
                }
            }
        }

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (Getcheckboxvalue(comboMenu, "UseREGapclose") && Q.IsReady() && W.IsReady() &&
             E.IsReady() && R.IsReady() && (Player.Distance(target) < Q.Range + E.Range * 2))
            {
                //CastRSmart(target);
                CastR(target);
                E.Cast(Player.ServerPosition.Extend(target.ServerPosition, E.Range - 1).To3D());
                E.Cast(Player.ServerPosition.Extend(target.ServerPosition, E.Range - 1).To3D());
                W.Cast();
                Q.Cast(target);
            }
            else
            {
                if (R.IsReady())
                {
                    if (Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        //CastRSmart(target);
                        CastR(target);
                    }

                    if (DamageToUnit(target) > target.Health)
                    {
                        //CastRSmart(target);
                        CastR(target);
                    }

                    if ((Q.IsReady() || E.IsReady()))
                    {
                        //CastRSmart(target);
                        CastR(target);
                    }

                    if (Player.IsInAutoAttackRange(target))
                    {
                        //CastRSmart(target);
                        CastR(target);
                    }
                }

                if (W.IsReady() && Getslidervalue(miscMenu, "UseWWhen") == 0 &&
                    (Q.IsReady() || Player.IsInAutoAttackRange(target)))
                {
                    W.Cast();
                }

                if (Q.IsReady())
                {
                    Q.Cast(target);
                }

                if (E.IsReady())
                {
                    E.Cast(target);
                }
            }
        }
        private static void Jungleclear()
        {
            var Monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, 1000f);

            if (Monsters != null)
            {
                if (Q.IsLearned && Q.IsReady())
                {

                    foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(Q.Range) && x.Health > 20))
                    {
                        Q.Cast(monster);
                    }
                    foreach (var monster in Monsters.Where(x => !x.IsDead && x.IsValidTarget(Q.Range) && x.Health > 30 &&
                                                                    !x.Name.ToLower().Contains("mini")))
                    {
                        Q.Cast(monster);
                    }
                }
            }

            if (W.IsLearned && W.IsReady())
            {
                int count = Monsters.Where(x => x.IsValidTarget(Player.GetAutoAttackRange()))
                                    .Count();

                W.Cast();
            }

            if (E.IsLearned && E.IsReady())
            {
                var List = Monsters.Where(x => !x.IsDead && x.IsValidTarget(E.Range * 2));
                Monsters.Where(x => !x.IsDead && x.IsValidTarget(E.Range * 2) && !x.Name.ToLower().Contains("mini"));
                foreach (var monster in List)
                {
                    if (monster.Distance(Player.Position) < 400)
                    {
                        E.Cast(monster.Position);
                    }
                    else
                    {
                        E.Cast(Player.Position.Extend(monster.Position, E.Range).To3D());
                        E.Cast(monster.Position);
                    }
                }
            }
        }


        public static bool CanKillWithUltCombo(AIHeroClient target)
        {
            return Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.W) + Player.GetSpellDamage(target, SpellSlot.R) >
                   target.Health;
        }

        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (LastHarassPos == null)
            {
                LastHarassPos = ObjectManager.Player.ServerPosition;
            }

            if (JumpBack)
            {
                E.Cast((Vector3)LastHarassPos);
            }

            if (W.IsReady() && Getslidervalue(miscMenu, "UseWWhen") == 0 &&
                    (Q.IsReady() || Player.IsInAutoAttackRange(target)))
            {
                W.Cast();
            }

            if (Q.IsReady())
            {
                Q.Cast(target);
            }
            if (E.IsReady() && Getslidervalue(harassMenu, "UseEHarassMode") == 1)
            {
                E.Cast(target);
            }
        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position, W.Range).ToList();
            if (minions != null)
            {
                if (Q.IsLearned && Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => !x.IsDead && x.IsValidTarget(Q.Range) && x.Health > 20))
                    {
                        Q.Cast(minion);
                    }
                }
                if (E.IsLearned && E.IsReady());
            }
        }
    }
}