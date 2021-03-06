﻿using System.Collections.Generic;
using System.Linq;
using General.Physics;
using NPC;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Items;
using Pipliz;
using Pipliz.JSON;
using Server;
using Server.AI;
using Server.NPCs;
using Shared;

namespace Pandaros.Settlers.Monsters.Bosses
{
    [ModLoader.ModManagerAttribute]
    public class FallenRanger : Zombie, IPandaBoss
    {
        public static string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.FallenRanger";
        private static NPCTypeMonsterSettings _mts;

        private static readonly Dictionary<ushort, int> REWARDS = new Dictionary<ushort, int>
        {
            {Mana.Item.ItemIndex, 10}
        };

        private double _cooldown = 2;

        private readonly float _totalHealth = 40000;

        public FallenRanger() :
            base(NPCType.GetByKeyNameOrDefault(Key), new Path(), new Players.Player(NetworkID.Invalid))
        {
        }

        public FallenRanger(Path path, Players.Player originalGoal) :
            base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var c  = Colony.Get(originalGoal);
            var ps = PlayerState.GetPlayerState(originalGoal);
            var hp = c.FollowerCount * ps.Difficulty.BossHPPerColonist;

            if (hp < _totalHealth)
                _totalHealth = hp;

            health = _totalHealth;
        }

        public IPandaBoss GetNewBoss(Path path, Players.Player p)
        {
            return new FallenRanger(path, p);
        }

        public string AnnouncementText => "I've got you in my sights!";
        public string DeathText => "Looks like I have to work on my aim.";
        public string Name => "Fallen Ranger";
        public override float TotalHealth => _totalHealth;

        public bool KilledBefore
        {
            get => false;
            set => killedBefore = value;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + "ZombieAudio";
        public float ZombieMultiplier => 1f;
        public float ZombieHPBonus => 0;
        public float MissChance => 0.05f;

        public Dictionary<ushort, int> KillRewards => REWARDS;

        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            {DamageType.Void, 20f},
            {DamageType.Physical, 30f}
        };

        public DamageType ElementalArmor => DamageType.Earth;

        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            {DamageType.Physical, 0.15f}
        };

        public override bool Update()
        {
            if (Time.SecondsSinceStartDouble > _cooldown)
            {
                if (Players.FindClosestAlive(Position, out var p, out var dis) &&
                    dis <= 30 &&
                    Physics.CanSee(Position, p.Position))
                {
                    Indicator.SendIconIndicatorNear(new Vector3Int(Position), ID,
                                                    new IndicatorState(2, GameLoader.Bow_Icon));

                    ServerManager.SendAudio(Position, "bowShoot");
                    p.Health -= Damage.Sum(kvp => kvp.Key.CalcDamage(DamageType.Physical, kvp.Value));
                    ServerManager.SendAudio(p.Position, "fleshHit");
                    _cooldown = Time.SecondsSinceStartDouble + 4;
                }
                else if (NPCTracker.TryGetNear(Position, 30, out var npc) &&
                         Physics.CanSee(Position, npc.Position.Vector))
                {
                    Indicator.SendIconIndicatorNear(new Vector3Int(Position), ID,
                                                    new IndicatorState(2, GameLoader.Bow_Icon));

                    ServerManager.SendAudio(Position, "bowShoot");

                    npc.OnHit(Damage.Sum(kvp => kvp.Key.CalcDamage(DamageType.Physical, kvp.Value)), this,
                              ModLoader.OnHitData.EHitSourceType.Monster);

                    ServerManager.SendAudio(npc.Position.Vector, "fleshHit");
                    _cooldown = Time.SecondsSinceStartDouble + 4;
                }
            }

            killedBefore = false;
            return base.Update();
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".Monsters.Bosses.FallenRanger.Register")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.server.loadnpctypes")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.registermonstertextures")]
        public static void Register()
        {
            var m = new JSONNode()
                   .SetAs("keyName", Key)
                   .SetAs("printName", "Fallen Ranger")
                   .SetAs("npcType", "monster");

            var ms = new JSONNode()
                    .SetAs("albedo", GameLoader.NPC_PATH + "FallenRanger.png")
                    .SetAs("normal", GameLoader.NPC_PATH + "ZombieQueen_normal.png")
                    .SetAs("emissive", GameLoader.NPC_PATH + "ZombieQueen_emissive.png")
                    .SetAs("initialHealth", 4000)
                    .SetAs("movementSpeed", 1.25f)
                    .SetAs("punchCooldownMS", 2000)
                    .SetAs("punchDamage", 100);

            m.SetAs("data", ms);
            _mts = new NPCTypeMonsterSettings(m);
            NPCType.AddSettings(_mts);
        }
    }
}