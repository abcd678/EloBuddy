using System;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Marksman_Buddy.Internal;

namespace Marksman_Buddy.Plugins
{
    internal class Twitch : PluginBase
    {
        private static Spell.Skillshot _W;
        private static Spell.Active _Q, _E;
        private static readonly string[] _Minions = {"SRU_Dragon", "SRU_Baron", "Sru_Crab", "Siege"};

        public Twitch()
        {
            _SetupMenu();
            _SetupSpells();
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void _SetupSpells()
        {
            _Q = new Spell.Active(SpellSlot.Q);
            _W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 250, 1400, 275);
            _E = new Spell.Active(SpellSlot.E, 1200);
        }

        private static void _SetupMenu()
        {
            Variables.Config.AddGroupLabel("Combo");
            Variables.Config.Add("Twitch.UseECombo", new CheckBox("Use E in Combo"));
            Variables.Config.Add("Twitch.UseEComboStacks", new Slider("Cast E at X Stacks", 5, 1, 5));
            Variables.Config.Add("Twitch.UseWCombo", new CheckBox("Use W in Combo"));
            Variables.Config.AddGroupLabel("Harrass");
            Variables.Config.Add("Twitch.UseEHarass", new CheckBox("Use E in Harass"));
            Variables.Config.Add("Twitch.UseEHarassStacks", new Slider("Cast E at X Stacks", 3, 1, 5));
            Variables.Config.Add("Twitch.UseWHarass", new CheckBox("Use W in Harass", false));
            Variables.Config.AddGroupLabel("Misc");
            Variables.Config.Add("Twitch.CastQEnemies", new Slider("Cast Q if X Enemies Around", 4, 3, 5));
            Variables.Config.Add("Twitch.KS", new CheckBox("Use E to KS"));
            Variables.Config.Add("Twitch.EExecute", new CheckBox("Use E to execute Large Minions"));
            Variables.Config.AddGroupLabel("Draw");
            Variables.Config.Add("Twitch.DrawAvailableSpells", new CheckBox("Draw only Available Spells"));
            Variables.Config.Add("Twitch.DrawW", new CheckBox("Draw W"));
            Variables.Config.Add("Twitch.DrawE", new CheckBox("Draw E"));
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Variables.ComboMode)
            {
                _Combo();
            }

            if (Variables.HarassMode)
            {
                _Harrass();
            }

            _QCount();
            _KillSteal();
            _Execute();
        }

        private static void _Execute()
        {
            foreach (var minion in 
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Position.Distance(ObjectManager.Player.Position) < 1200))
            {
                if (minion.Team == ObjectManager.Player.Team || !_ContainsSimiliar(_Minions, minion.Name))
                {
                    continue;
                }

                if (_ECanKill(minion, _E) &&
                    Variables.Config["Twitch.EExecute"].Cast<CheckBox>().CurrentValue)
                {
                    _E.Cast();
                }
            }
        }

        private static bool _ContainsSimiliar(string[] array, string simily) //Who needs comparators Kappa
        {
            return array.Any(element => element.ToLower().Contains(simily.ToLower()));
        }

        private static void _Harrass()
        {
            var WTarget = TargetSelector.GetTarget(_W.Range, DamageType.True);
            if (Variables.Config["Twitch.UseWHarass"].Cast<CheckBox>().CurrentValue
                && !_W.IsOnCooldown)
            {
                _W.Cast(WTarget);
            }

            if (!Variables.Config["Twitch.UseEHarass"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            foreach (var hero in
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.Position.Distance(ObjectManager.Player.Position) < 1200))
            {
                if (hero.GetBuffCount("twitchdeadlyvenom") >=
                    Variables.Config["Twitch.UseEHarassStacks"].Cast<Slider>().CurrentValue)
                {
                    _E.Cast();
                }
            }
        }

        private static void _KillSteal()
        {
            foreach (var hero in
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.Position.Distance(ObjectManager.Player.Position) < 1200))
            {
                if (_ECanKill(hero, _E) && Variables.Config["Twitch.KS"].Cast<CheckBox>().CurrentValue)
                {
                    _E.Cast();
                }
            }
        }

        private static void _Combo()
        {
            var WTarget = TargetSelector.GetTarget(_W.Range, DamageType.True);
            if (Variables.Config["Twitch.UseWCombo"].Cast<CheckBox>().CurrentValue
                && !_W.IsOnCooldown)
            {
                _W.Cast(WTarget);
            }

            if (!Variables.Config["Twitch.UseECombo"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            foreach (var hero in
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.Position.Distance(ObjectManager.Player.Position) < 1200))
            {
                if (hero.GetBuffCount("twitchdeadlyvenom") >=
                    Variables.Config["Twitch.UseEComboStacks"].Cast<Slider>().CurrentValue)
                {
                    _E.Cast();
                }
            }
        }

        private static void _QCount()
        {
            if (
                HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(3000))
                    .Count(enemy => ObjectManager.Player.Distance(enemy.Path.Last()) < 600) >=
                Variables.Config["Twitch.CastQEnemies"].Cast<Slider>().CurrentValue)
            {
                _Q.Cast();
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (((Variables.Config["Twitch.DrawAvailableSpells"].Cast<CheckBox>().CurrentValue && _W.IsReady()) ||
                 !Variables.Config["Twitch.DrawAvailableSpells"].Cast<CheckBox>().CurrentValue) &&
                Variables.Config["Twitch.DrawW"].Cast<CheckBox>().CurrentValue)
            {
                new Circle {Color = Color.Orange, Radius = _W.Range}.Draw(ObjectManager.Player.Position);
            }

            if (((Variables.Config["Twitch.DrawAvailableSpells"].Cast<CheckBox>().CurrentValue && _E.IsReady()) ||
                 !Variables.Config["Twitch.DrawAvailableSpells"].Cast<CheckBox>().CurrentValue) &&
                Variables.Config["Twitch.DrawE"].Cast<CheckBox>().CurrentValue)
            {
                new Circle {Color = Color.DarkGreen, Radius = _E.Range}.Draw(ObjectManager.Player.Position);
            }
        }

        private static bool _ECanKill(Obj_AI_Base hero, Spell.Active _E)
        {
            var EDamage = Player.Instance.GetSpellDamage(hero, SpellSlot.E) - 20.0f;
            return EDamage > hero.Health;
        }
    }
}
