using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Events;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace RinSatsukiRedux.NPCs
{
    [AutoloadHead]
    [LegacyName("SCRAPPED")] // ;)
    public class Kirin : ModNPC
    {
        private const float KirinDiscount = 0.5f;
        
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 23;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 500;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 60;
            NPCID.Sets.AttackAverageChance[Type] = 10;
            NPCID.Sets.ShimmerTownTransform[Type] = false;
            NPC.Happiness
                .SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
                .SetBiomeAffection<JungleBiome>(AffectionLevel.Love)
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Hate)
                .SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
                .SetNPCAffection(NPCID.Nurse, AffectionLevel.Dislike)
                .SetNPCAffection(NPCID.Dryad, AffectionLevel.Like);
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifiers);
            
        }

        // attributes for this npc
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.lavaImmune = true;
            NPC.trapImmune = true;
            NPC.width = 18;
            NPC.height = 44;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 777;
            NPC.HitSound = new SoundStyle("RinSatsukiRedux/Sounds/NPCHit/rinHurt")
            {
                Volume = 0.5f
            };
            NPC.DeathSound = new SoundStyle("RinSatsukiRedux/Sounds/NPCHit/rinDie")
            {
                Volume = 0.33f
            };
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.PartyGirl;
        }

        // bestiary entry with a reference to the localization
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.RinSatsukiRedux.Bestiary.Kirin")
            });
        }
        
        // can the npc spawn. normally would include some condition, but satsuki is always available. 
        public override bool CanTownNPCSpawn(int numTownNpCs)
        {
            return true;
        }
        
        public override List<string> SetNPCNameList() => new List<string>()
        {
            "Rin Satsuki"
        };

        // NPC Dialogue Selection/Unlocking
        public override string GetChat()
        {
            // 1 in 727 chance for Bad Apple!
            if (Main.rand.NextBool(727))
                return this.GetLocalizedValue("Chat.EasterEgg");
            
            // If bloodmoon
            if (Main.bloodMoon)
                return this.GetLocalizedValue("Chat.BloodMoon" + Main.rand.Next(1, 4 + 1));

            WeightedRandom<string> dialogue = new WeightedRandom<string>();

            // Always available dialogue
            dialogue.Add(this.GetLocalizedValue("Chat.Normal1"));
            dialogue.Add(this.GetLocalizedValue("Chat.Normal2"));

            // Night dialogue
            if (!Main.dayTime)
            {
                dialogue.Add(this.GetLocalizedValue("Chat.Night1"));
                dialogue.Add(this.GetLocalizedValue("Chat.Night2"));
            }
            
            // If Nurse is nearby
            int nurseIndex = NPC.FindFirstNPC(NPCID.Nurse);
            if (nurseIndex != -1)
                dialogue.Add(this.GetLocalization("Chat.Nurse").Format(Main.npc[nurseIndex].GivenName));

            // If Dryad is nearby
            int dryadIndex = NPC.FindFirstNPC(NPCID.Dryad);
            if (dryadIndex != -1)
                dialogue.Add(this.GetLocalization("Chat.Dryad").Format(Main.npc[dryadIndex].GivenName));
            
            // If in Jungle
            if (Main.LocalPlayer.ZoneJungle)
                dialogue.Add(this.GetLocalizedValue("Chat.Jungle"));

            // If party active
            if (BirthdayParty.PartyIsUp)
                dialogue.Add(this.GetLocalizedValue("Chat.Party"));

            // if world is hard
            if (Main.hardMode)
            {
                dialogue.Add(this.GetLocalizedValue("Chat.Hardmode1"));
            }
            
            // if tentacle monstrosity is dead
            if (NPC.downedMoonlord)
            {
                dialogue.Add(this.GetLocalizedValue("Chat.MoonLordDefeated1"));
                dialogue.Add(this.GetLocalizedValue("Chat.MoonLordDefeated2"));
            }

            return dialogue;
        }
        
        public static bool IsDebuffed()
        {
            // iterate for a debuff over 1 second of time remaining
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int num2 = Main.LocalPlayer.buffType[i];
                if (Main.debuff[num2] && Main.LocalPlayer.buffTime[i] > 60 && (num2 < 0 || num2 >= BuffID.Count || !BuffID.Sets.NurseCannotRemoveDebuff[num2]))
                {
                    return true; // yes debuff
                }
            }
            return false; // no debuff
        }

        // Heal Debuffs
        public void HealDebuffs()
        {
            //// Modified Terraria Source, different in the way that it doesn't check, but removes debuffs.
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int num = Main.LocalPlayer.buffType[i];
                if (Main.debuff[num] && Main.LocalPlayer.buffTime[i] > 0 && (num < 0 || num >= BuffID.Count || !BuffID.Sets.NurseCannotRemoveDebuff[num]))
                {
                    Main.LocalPlayer.DelBuff(i);
                    i = -1;
                }
            }
            //// 
        }
        
        //// Terraria's NurseHeal Code (modified)
        public static int GetNurseHealCost()
        {
            int num = Main.LocalPlayer.statLifeMax2 - Main.LocalPlayer.statLife;
            
            // iteratively add extra cost for all debuffs above a threshold of 60 seconds, provided it is not an irremovable debuff
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int num2 = Main.LocalPlayer.buffType[i];
                if (Main.debuff[num2] && Main.LocalPlayer.buffTime[i] > 60 && (num2 < 0 || num2 >= BuffID.Count || !BuffID.Sets.NurseCannotRemoveDebuff[num2]))
                {
                    num += 100;
                }
            }
            
            // tentacle monstrosity dead and CalamityMod is active, mult by 300
            // TryGetMod safely
            if (NPC.downedMoonlord && ModLoader.TryGetMod("CalamityMod", out Mod Calamity))
            {
                num *= 250;
            }
            
            // multiply the cost by 200 if golem is dead
            if (NPC.downedGolemBoss)
            {
                num *= 200;
            }
            
            // multiply the cost if plant dead
            else if (NPC.downedPlantBoss)
            {
                num *= 150;
            }
            
            // any mech boss dead, mult by 100
            else if (NPC.downedMechBossAny)
            {
                num *= 100;
            }
            
            // hardmode, mult by 60
            else if (Main.hardMode)
            {
                num *= 60;
            }
            
            // skelly or bee, mult by 25
            else if (NPC.downedBoss3 || NPC.downedQueenBee)
            {
                num *= 25;
            }
            
            // boc or eow, mult by 10
            else if (NPC.downedBoss2)
            {
                num *= 10;
            }
            
            // eoc, mult by 3
            else if (NPC.downedBoss1)
            {
                num *= 3;
            }
            
            // world is expert, mult by 2
            if (Main.expertMode)
            {
                num *= 2;
            }
            
            // apply discount if possible
            if (Main.LocalPlayer.discountAvailable)
            {
                num = (int)(num * 0.8f);
            }
            // final result is returned to caller.
            return (int)((float)num * Main.LocalPlayer.currentShoppingSettings.PriceAdjustment);
        }
        ////

        // Literally the Nurse heal but cheaper (wow so cool and amazing)
        public string Heal()
        {
            // if currHP != maxHP, do the corresponding heal
            if ((Main.LocalPlayer.statLife != Main.LocalPlayer.statLifeMax2) || IsDebuffed())
            {
                int missingHealth = Main.LocalPlayer.statLifeMax2 - Main.LocalPlayer.statLife;
                
                // cost math
                // Kirin final discount. 50% cheaper than the Nurse.
                int cost = (int) (GetNurseHealCost() * KirinDiscount);
                if (Main.LocalPlayer.CanAfford(cost))
                {
                    Main.LocalPlayer.BuyItem(cost);
                    // do the heal
                    Main.LocalPlayer.Heal(missingHealth);
                    SoundEngine.PlaySound(SoundID.Item4);
                    
                    // wipe debuffs... except Rin is faster and can freeze all the player's debuffs.
                    HealDebuffs();
                    
                    return this.GetLocalizedValue("didHeal");
                    
                    
                }
                else
                {
                    // Zero [KROMER]
                    return this.GetLocalizedValue("brokeAssMf");
                }
            }
            
            // otherwise, reject them
            else
            {
                return this.GetLocalizedValue("failedHeal");
            }
            
        }
        
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            
            // if the player's health is not currently max or they are under a debuff, present them with the correct heal display
            if ((Main.LocalPlayer.statLife != Main.LocalPlayer.statLifeMax2) || IsDebuffed())
                button2 = this.GetLocalizedValue("HealButtonWithCost").FormatWith(Main.ValueToCoins((long) (GetNurseHealCost() * KirinDiscount)));
            else
            // otherwise, use the default heal display.
            {
                button2 = this.GetLocalizedValue("HealButton");
            }
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
            }
            else
            {
                Main.npcChatText = Heal();
            }
        }

        public override void AddShops()
        {
            NPCShop shop = new(Type);

            shop.Add(new Item(ItemID.Daybloom) { shopCustomPrice = 133 })
                .Add(new Item(ItemID.Blinkroot) { shopCustomPrice = 133 })
                .Add(new Item(ItemID.Waterleaf) { shopCustomPrice = 133 })
                .Add(new Item(ItemID.Shiverthorn) { shopCustomPrice = 133 })
                .Add(new Item(ItemID.Moonglow) { shopCustomPrice = 133 }, Condition.DownedSkeletron)
                .Add(new Item(ItemID.Deathweed) { shopCustomPrice = 133 }, Condition.DownedEowOrBoc)
                .Add(new Item(ItemID.Fireblossom) { shopCustomPrice = 133 }, Condition.DownedSkeletron)
                .Add(new Item(ItemID.Mushroom) { shopCustomPrice = 44 })
                .Add(new Item(ItemID.GlowingMushroom) { shopCustomPrice = 67 })
                .Add(new Item(ItemID.VileMushroom) { shopCustomPrice = 44 }, Condition.DownedEowOrBoc)
                .Add(new Item(ItemID.ViciousMushroom) { shopCustomPrice = 44 }, Condition.DownedEowOrBoc)

                // You found the [Moss]!
                .Add(new Item(ItemID.GreenMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.BrownMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.RedMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.BlueMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.PurpleMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.LavaMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.KryptonMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.XenonMoss) { shopCustomPrice = 133 }, Condition.Hardmode)
                .Add(new Item(ItemID.ArgonMoss) { shopCustomPrice = 133 }, Condition.Hardmode)

                .Add(new Item(ItemID.PumpkinSeed) { shopCustomPrice = 67 })
                .Add(new Item(ItemID.Sunflower) { shopCustomPrice = 44 })
                .Add(new Item(ItemID.LifeFruit) { shopCustomPrice = 13333 }, Condition.DownedPlantera);
                
            shop.Register();
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Point_Item").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Point_Item").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Point_Item").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Point_Item").Type, 1f);
                }
            }
        }

        // Make this Town NPC teleport to the Queen Statue.
        public override bool CanGoToStatue(bool toQueenStatue) => true;

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 33;
            if (Main.hardMode)
            {
                damage *= 2;
            }
            knockback = 0f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 0;
            randExtraCooldown = 0;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ProjectileID.CrystalStorm;
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 18f;
        }
    }
}
