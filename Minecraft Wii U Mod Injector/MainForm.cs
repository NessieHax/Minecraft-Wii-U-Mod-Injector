using MetroFramework.Controls;
using MetroFramework.Forms;
using MetroFramework;
using Minecraft_Wii_U_Mod_Injector.Helpers;
using Minecraft_Wii_U_Mod_Injector.Forms;
using Minecraft_Wii_U_Mod_Injector.Helpers.Files;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Minecraft_Wii_U_Mod_Injector.CEMU;
using Minecraft_Wii_U_Mod_Injector.Helpers.Win_Forms;
using Minecraft_Wii_U_Mod_Injector.Properties;
using static System.Windows.Forms.DialogResult;
using Memory = Minecraft_Wii_U_Mod_Injector.CEMU.CemuMemory;
using Settings = Minecraft_Wii_U_Mod_Injector.Helpers.Files.Settings;

namespace Minecraft_Wii_U_Mod_Injector
{
    public partial class MainForm : MetroForm
    {
        #region base variables

        #region references

        public static CEMU.CemuMemory CemuMem;

        #endregion
        
        #region bools
        public bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                DiscordRp.SetPresence(_isConnected ? "Connected" : "Disconnected", MainTabs.SelectedTab.Text + " tab");
            }
        }

        private bool _tooHighWarn;
        #endregion

        #region assembly

        public uint On = 0x38600001;
        public uint Off = 0x38600000;
        public uint On2 = 0x3BE00001;
        public uint Off2 = 0x3BE00000;
        public uint Blr = 0x4E800020;
        public uint Mflr = 0x7C0802A6;
        public uint Nop = 0x60000000;

        #endregion

        #region cemu
        public string cemuAOB = "60 00 00 00 4E 80 00 20 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 2C 05 00 00 7C 6C 1B 78 41 82 01 3C 7D 04 60 50";
        public string cemuEntry;
        #endregion

        #region pointers
        private uint _localPlayer;
        #endregion

        #endregion
        public MainForm()
        {
            InitializeComponent();
        }

        private void Init(object sender, EventArgs e)
        {
            _ = new States(this);
            _ = new Messaging(this);
            _ = new Setup(this);

            Setup.SetupInjector();
        }


        private void Exit(object sender, FormClosingEventArgs e)
        {
            DiscordRp.Deinitialize();
            Settings.Write("TabIndex", MainTabs.SelectedIndex.ToString(), "Display");
        }
        
        private void SwapTab(object sender, EventArgs e)
        {
            var tile = (MetroTile)sender;

            if (MainTabs.SelectedIndex != tile.TileCount)
                MainTabs.SelectedIndex = tile.TileCount;
            else
                return;

            DiscordRp.SetPresence(IsConnected ? "Connected" : "Disconnected", MainTabs.SelectedTab.Text + " tab");
        }

        async Task PutTaskDelay(int sleep)
        {
            await Task.Delay(sleep);
        }

        private async void ConnectBtnClicked(object sender, EventArgs e)
        {
            try
            {
                CemuMem = new CemuMemory();

                switch (States.CurrentState)
                {
                    case States.StatesIds.Disconnected:
                        States.SwapState(States.StatesIds.Connecting);
                        CemuMem.Attach("Cemu.exe");
                        cemuEntry = await CemuMem.GetEntryPointAsync(cemuAOB);
                        States.SwapState(States.StatesIds.Connected);
                        IsConnected = true;
                        break;

                    case States.StatesIds.Connected:
                        CemuMem.Detach();
                        IsConnected = false;
                        States.SwapState(States.StatesIds.Disconnected);
                        break;
                }
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Unable to Connect to CEMU, unknown error", false);
                States.SwapState(States.StatesIds.Disconnected);
            }
        }

        private void FormThemeSelected(object sender, EventArgs e)
        {
            try
            {
                Theme = (MetroThemeStyle) Enum.Parse(typeof(MetroThemeStyle), themeBox.Text);
                StyleMngr.Theme = (MetroThemeStyle) Enum.Parse(typeof(MetroThemeStyle), themeBox.Text);
                Refresh();
                Settings.Write("Theme", themeBox.Text, "Display");
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Change Injector Form Theme", true);
            }
        }

        private void FormColorSelected(object sender, EventArgs e)
        {
            try
            {
                Style = (MetroColorStyle) Enum.Parse(typeof(MetroColorStyle), colorsBox.Text);
                StyleMngr.Style = (MetroColorStyle) Enum.Parse(typeof(MetroColorStyle), colorsBox.Text);
                Refresh();
                Settings.Write("Color", colorsBox.Text, "Display");
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Change Injector Form Color", true);
            }
        }

        private void DiscordRpcToggleChecked(object sender, EventArgs e)
        {
            try
            {
                if (discordRpcCheckBox.Checked)
                {
                    DiscordRp.Initialize();
                    DiscordRp.SetPresence(IsConnected ? "Connected" : "Disconnected",
                        MainTabs.SelectedTab.Text + " tab");
                }
                else
                {
                    DiscordRp.Deinitialize();
                }

                Settings.Write("DiscordRPC", discordRpcCheckBox.Checked.ToString(), "Discord");
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Toggle Discord RPC", true);
            }
        }

        private void ResetConfigClicked(object sender, EventArgs e)
        {
            try
            {
                if (Messaging.Show("Resetting the configuration file will reset all your preferences, continue?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == Yes)
                {
                    if (File.Exists(Application.StartupPath + "/Minecraft Wii U Mod Injector.ini"))
                        File.Delete(Application.StartupPath + "/Minecraft Wii U Mod Injector.ini");
                    else
                        Messaging.Show("Configuration File doesn't exist, nothing to reset.");
                }
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Reset Config", true);
            }
        }

        private void ReleaseNotesToggleClicked(object sender, EventArgs e)
        {
            try
            {
                if (releaseNotesToggle.Checked)
                {
                    BuildNotesBox.Text = Resources.releaseNote;
                    Settings.Write("ReleaseNotes", "current", "Display");
                }
                else
                {
                    BuildNotesBox.Text = Resources.releaseNotes;
                    Settings.Write("ReleaseNotes", "all", "Display");
                }
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Toggle Release Notes", true);
            }
        }

        private void CheckForPreReleaseToggled(object sender, EventArgs e)
        {
            try
            {
                Settings.Write("PrereleaseOptIn", CheckForPreRelease.Checked.ToString(), "Updates");
            }
            catch (Exception error)
            {
                Exceptions.LogError(error, "Failed to Toggle Release Notes", true);
            }
        }

        private void OpenLangMngrBtnClicked(object sender, EventArgs e)
        {
            new LanguageMngr(this).ShowDialog();
        }

        private void QuickModsManagerBtnClicked(object sender, EventArgs e)
        {
            new QuickModsMngr(this).ShowDialog();
        }

        private void OpenFaqInfoClicked(object sender, EventArgs e)
        {
            new Faq(this).ShowDialog();
        }

        private void SetupTutorialBtnClicked(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/watch?v=be5fNSgxhrU");
        }

        private void DiscordSrvBtnClicked(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/jrzZWaDc7a");
        }

        private void CreditsBtnClicked(object sender, EventArgs e)
        {
            new Credits(this).ShowDialog();
        }

        private void ItemIdHelpBtnClicked(object sender, EventArgs e)
        {
            Messaging.Show(MessageBoxIcon.Information, "Item IDs can be found at https://minecraft-ids.grahamedgecombe.com/ \nData Values are the numbers behind the : in the ID.\nFor example, if you want Birch Wood the ID would be 17 and the data value would be 2");
        }

        #region memory editing

        private void InsaneCriticalHitsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x106D0D4C, 0x43F00000, 0x3FC00000, InsaneCriticalHits.Checked);
        }

        private void AlwaysSwimmingToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteULongToggle(0x023405EC, 0x386000014E800020, 0x3D800233398C69B8, AlwaysSwimming.Checked); //Entity::isSwimming((void))
        }

        private void InfiniteRiptideToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0232C210, On, Off, InfiniteRiptide.Checked); //Entity::isInWaterOrRain((void))
        }

        private void FullRotationToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteULongToggle(0x105DDAF8, 0xC3B4000043B40000, 0xC2B4000042B40000, FullRotation.Checked);
        }

        private void AlwaysDamagedPlayersToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x027206CC, On, 0x57E3063E, AlwaysDamagedPlayers.Checked); //Player::isInWall((void))
        }

        private void InfiniteItemsToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteLongToggle(0x024872AC, 0x600000004E800020, 0x3D800248398C7294, InfiniteItems.Checked); //ItemInstance::shrink((int))
        }

        private void RapidBowToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02162F04, 0x3BE00001, 0x3BE00014, RapidBow.Checked); //GetMaxBowDuration__7BowItemSFv
        }

        private void BloodVisionToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0257E000, Off, 0x57E3063E, BloodVision.Checked); //LivingEntity::isAlive((void))
        }

        private void IgnorePotionsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0258E4BC, Off, On, IgnorePotions.Checked); //LivingEntity::isAffectedByPotions((void))
        }

        private void ForeverLastingPotionsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02576994, Blr, 0x9421FF40, ForeverLastingPotions.Checked); //LivingEntity::tickEffects((void))
        }

        private void BypassInvulnerabilityToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0225458C, On, 0x88630009, BypassInvulnerability.Checked); //DamageSource::isBypassInvul(const(void))
        }

        private void PlaceBlocksonHeadToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0207F604, On, 0x7FC3F378, PlaceBlocksonHead.Checked); //ArmorSlot::mayPlace((not_null_ptr__tm__15_12ItemInstance))
        }

        private void WalkonWaterToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteULongToggle(0x025A963C, 0x3C60109E80639948, 0x3C6010568063B61C, WalkonWater.Checked); //LiquidBlock::getClipAABB((BlockState const *, LevelSource *, BlockPos const &))
        }

        private void AlwaysElytraToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteULongToggle(0x0258DACC, 0x386000014E800020, 0x3D800233398C69B8, AlwaysElytra.Checked); //LivingEntity::isFallFlying((void))
        }

        private void CaveFinderToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x030E4924, 0x38800000, 0x38800001, CaveFinder.Checked); //enableState__14GlStateManagerSFi
        }

        private void WallhackToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x031B2B4C, On, 0x5403063E, Wallhack.Checked); //renderDebug__9MinecraftSFv
        }

        private void LargeXpDropsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0239A26C, Nop, 0x4180000C, LargeXPDrops.Checked); //getExperienceValue__13ExperienceOrbSFi:
            ////GeckoU.WriteUIntToggle(0x0239A270, 0x38607FFF, 0x386009AD, LargeXPDrops.Checked);
        }

        private void WallClimbingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0257DF90, On, 0x57E3063E, WallClimbing.Checked); //LivingEntity::onLadder((void))
        }

        private void NoCollisionToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x020E7BF4, Blr, 0x9421FFE0, NoCollision.Checked); //Block::addCollisionAABBs((BlockState const *, Level *, BlockPos const &, AABB const *, std::vector__tm__39_P4AABBQ2_3std23allocator__tm__7_P4AABB *, boost::shared_ptr__tm__8_6Entity, bool))
        }

        private void InfiniteAirToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02572AA4, Blr, Mflr, InfiniteAir.Checked); //LivingEntity::decreaseAirSupply((int))
        }

        private void InfiniteDurabilityToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0248909C, Nop, 0x4181001C, InfiniteDurability.Checked); //ItemInstance::hurt((int,Random *)) (bgt)
            ////GeckoU.WriteUIntToggle(0x024889CC, Off, On, InfiniteDurability.Checked); //ItemInstance::isDamageableItem((void))
        }

        private void SuperKnockbackToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0257D980, 0xFC00E838, 0xFC006378, SuperKnockback.Checked); //Knockback__12LivingEntityFQ2_5boost25shared_ptr__tm__8_6EntityfdT3
            ////GeckoU.WriteUIntToggle(0x0257D990, 0xFD20B890, 0xFD290372, SuperKnockback.Checked);
        }

        private void DisabledKnockbackToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0257D85C, Blr, 0x9421FFA8, DisabledKnockback.Checked); //Knockback__12LivingEntityFQ2_5boost25shared_ptr__tm__8_6EntityfdT3
        }

        private void SilkTouchAnythingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x020EA77C, On, 0x57C3063E, SilkTouchAnything.Checked); //Block::isSilkTouchable((void))
        }

        private void DeveloperModeToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F5C874, On, Off, DeveloperMode.Checked); //CMinecraftApp::DebugArtToolsOn((void))
        }

        private void PickLiquidBlocksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x025A98504, On, 0x57C3063E, PickLiquidBlocks.Checked); //LiquidBlock::mayPick((BlockState const *, bool))
        }

        private void DuelWieldanyItemToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x024FD7F4, On, Off, DuelWieldanyItem.Checked); //_12ItemInstance::mayPlace__Q2_13InventoryMenu11OffhandSlotF35not_null_ptr__tm(void)
        }

        private void DisableStarvingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0271BA6C, On, 0x7FE3FB78, DisableStarving.Checked); //Player::isAllowedToIgnoreExhaustion((void))
        }

        private void InstantMiningToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x020E75C8, 0xC0230000, 0xC0230040, InstantMining.Checked); //Block::getDestroySpeed((BlockState const *, Level *, BlockPos const &))
        }

        private void FlyingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0271AA74, On, 0x7FE3FB78, Flying.Checked); //Player::isAllowedToFly((void))
        }

        private void DisableSuffocatingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x027206CC, Off, 0x57E3063E, DisableSuffocating.Checked); //Player::isInWall((void))
        }

        private void NoFallDamageToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02723540, Blr, 0x9421FFB0, NoFallDamage.Checked); //causeFallDamage__6PlayerFfT1
        }

        private void CraftAnythingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F70970, On, Off, CraftAnything.Checked); //IsDebugSettingEnabled__12GameSettingsSF13eDebugSettingi
            ////GeckoU.WriteUIntToggle(0x032283CC, 0x38800000, 0x38800001, CraftAnything.Checked); //run__15MinecraftServerFLPv
        }

        private void CreativeModeToggled(object sender, EventArgs e)
        {
            CemuMem.WriteBytes(cemuEntry, "2256F4C", "0x38 0x60 0x00 0x00"); //GameType::isCreative(const(void))
        }

        private void NoFogToggled(object sender, EventArgs e)
        {
            CemuMem.WriteBytes(cemuEntry, "10E4954", "0x38 0x60 0x00 0x00");
        }

        private void StaticLiquidBlocksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x022B9B8C, Blr, 0x9421FF00, StaticLiquidBlocks.Checked); //DynamicLiquidBlock::maintick((Level *, BlockPos const &, BlockState const *, Random *))
        }

        private void FoggyWeatherToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x031C06A4, On, Off, FoggyWeather.Checked); //isInCloud__13LevelRendererFdN21f
        }

        private void GenerateAmplifiedWorldToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x106CB93C, 0x4000000, 0x3DCCCCCD, GenerateAmplifiedWorld.Checked);
        }

        private void AcidLiquidBlocksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x022B8EC4, On, 0x57E3063E, AcidLiquidBlocks.Checked); //DynamicLiquidBlock::canSpreadTo((Level *, BlockPos const &, BlockState const *))
        }

        private void DisableCreativeFlagToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x025BF44C, On, 0x886300A9, DisableCreativeFlag.Checked); // labeled as "delete", so unsure if this still works
        }

        private void GeneratePlainWorldToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0214DB38, Blr, 0x9421FD80, GeneratePlainWorld.Checked); //BiomeDecorator::decorate((Biome *, Level *, Random *))
        }

        private void FreezingWorldToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0253C71C, On, Off, FreezingWorld.Checked); //Level::shouldFreeze((BlockPos const &, bool))
        }

        private void UncapEntitySpawnLimitToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0256CDFC, On, 0x5403063E, UncapEntitySpawnLimit.Checked); //Level::canCreateMore((eINSTANCEOF,Level::ESPAWN_TYPE))
        }

        private void AllDlcUnlockedToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02AE8B30, On, 0x7FE3FB78, AllDLCUnlocked.Checked); //DLCManager::IsDlcPackTemporarilyFree((int))
            ////GeckoU.WriteUIntToggle(0x02AEF828, On, 0x7FE3FB78, AllDLCUnlocked.Checked); //DLCManager::IsMiniGameTemporarilyFree((int))
        }

        private void HostOptionsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F17714, 0x38807D00, 0x388303F4, HostOptions.Checked); //CMinecraftApp::SetGameHostOption((eGameHostOption, unsigned int))
        }

        private void DisablePermanentKicksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0340D428, Blr, Mflr, DisablePermanentKicks.Checked); //CProfile::QuadrantOfflineXUID((int))
        }

        private void EnchantmentLevelSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteUInt(0x022F4900, GeckoU.Mix(0x38600000, EnchantmentLevelSlider.Value)); //getenchentmentslevel
        }

        private void JumpHeightSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x1066AAD4, (float)JumpHeightSlider.Value);
        }

        private void ReachSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x108F5620, Convert.ToSingle(ReachSlider.Value));
            //GeckoU.WriteFloat(0x108E0BDC, Convert.ToSingle(ReachSlider.Value));
        }

        private void RiptideFlyingSpeedSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x1079C3E0, (float)RiptideFlyingSpeedSlider.Value);
        }

        private void SprintingSpeedScaleSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x10692528, (float)SprintingSpeedScaleSlider.Value);
        }

        private void VisibleHitboxesToggled(object sender, EventArgs e)
        {
            //////GeckoU.WriteUIntToggle(whiteBoxAddress + 0x90, 0x0001F000, 0x000100000, VisibleHitboxes.Checked);
        }

        private void BypassFriendsOnlyToggled(object sender, EventArgs e)
        {
            //GeckoU.WriteULongToggle(0x02D5731C, 0x386000014E800020, 0x7C0802A69421FFF0, BypassFriendsOnly.Checked); //CGameNetworkManager::IsInPublicJoinableGame((void))
        }
        private void WalkingSpeedScaleChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x1066ACC8, (float)WalkingSpeedScaleSlider.Value);
        }

        private void CraftingTableAnywhereToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F59534, 0x480002E8, 0x7C0802A6, CraftingTableAnywhere.Checked);
        }

        private void HiddenGameModesUnlockedToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(pointer, 0x5, 0x2, HiddenGameModesUnlocked.Checked);
        }
        private void FieldOfViewSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x108911B8, (float)FieldOfViewSlider.Value);
        }

        private void TakeEverythingAnywhereToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02DEC0B4, On, 0x57E3063E, TakeEverythingAnywhere.Checked);
        }

        private void ArmorHudToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02E9B1B0, 0x7FE4FB78, 0x7FC4F378, ArmorHUD.Checked);
        }

        private void SlowMotionToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x109473C8, 0x3F700000, 0x3FF00000, SlowMotion.Checked);
        }

        private void DeadMauFiveModeToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F5D53C, On, Off, DeadMauFiveMode.Checked);
        }

        private void PlayerModelScaleSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x1091B90C, Convert.ToSingle(playerModelScaleSlider.Value));
        }

        private void GamepadSplitscreenToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F6EFFC, Off, 0x8863072A, GamepadSplitscreen.Checked); //CConsoleMinecraftApp::IsDrcPreventingSplitscreen((void))
        }

        private void DisableTeleportingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0336367C, Blr, 0x9421FED0, DisableTeleporting.Checked); //TeleportCommand::execute
        }

        private void GodModeToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02727824, Nop, 0xFC20F890, GodMode.Checked); //Player::getAbsorptionAmount((void))
        }

        private void GodModeAllToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x025795C0, Nop, 0xFC20F890, GodMode.Checked); //LivingEntity::setHealth((float))
        }

        private void FieldOfViewSplitSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteFloat(0x108C7870, (float)FieldOfViewSplitSlider.Value);
        }

        private void UnderwaterEffectsToggled(object sender, EventArgs e)
        {
            switch (UnderwaterEffects.CheckState)
            {
                case CheckState.Unchecked:
                    //GeckoU.WriteUInt(0x0253C3CC, 0x88630011);
                    UnderwaterEffects.Text = "Underwater Effects (Default)";
                    break;

                case CheckState.Checked:
                    //GeckoU.WriteUInt(0x0253C3CC, On);
                    UnderwaterEffects.Text = "Underwater Effects (On)";
                    break;

                case CheckState.Indeterminate:
                    //GeckoU.WriteUInt(0x0253C3CC, Off);
                    UnderwaterEffects.Text = "Underwater Effects (Off)";
                    break;
            }
        }

        private void HideBlocksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02048160, On, Off, HideBlocks.Checked);
        }

        private void AlwaysInLavaToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0255F818, 0x38000001, 0x38000000, AlwaysInLava.Checked);
        }

        private void SeeThroughBlocksToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x020E751C, 0x3BE00000, 0x3BE00001, SeeThroughBlocks.Checked);
        }

        private void LevelXToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02B47934, 0x2C1D0001, 0x2C1D0000, LevelX.Checked);
        }

        private void AlwaysInWaterToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x0255E81C, 0x38000001, 0x38000000, AlwaysInWater.Checked);
        }

        private void EspToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x031B2B4C, 0x38000001, 0x6CC08000, ESP.Checked);

            ////GeckoU.WriteUIntToggle(0x030EA178, 0xFC80E890, 0xFC60E890, ESP.Checked);
            ////GeckoU.WriteUIntToggle(0x030EA0E8, 0xFC80F890, 0xFC60F890, ESP.Checked);
            ////GeckoU.WriteUIntToggle(0x030EA0E4, 0xFC80F890, 0xFC40F890, ESP.Checked);
            ////GeckoU.WriteUIntToggle(0x030EA1C8, 0x6CC00000, 0x6CC08000, ESP.Checked);
        }

        private void NoSlowDownsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x108E0E60, 0x3F800000, 0x3E4CCCCD, NoSlowDowns.Checked);
        }


        private void SuperchargedCreepersToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02275934, On, 0x8869000C, SuperchargedCreepers.Checked);
        }

        private void IgnitedCreepersToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02273030, On, 0x8869000C, IgnitedCreepers.Checked);
        }

        private void ZombieTowerToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02A3B77C, On, 0x88630740, ZombieTower.Checked);
        }

        private void SunProofMobsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02A3D808, Off, On, SunProofMobs.Checked);
        }

        private void PotionAmplifierSliderChanged(object sender, EventArgs e)
        {
            if (PotionAmplifierSlider.Value == 0)
            {
                //GeckoU.WriteUInt(0x02692DF0, 0x80630008);
                return;
            }

            //GeckoU.WriteUInt(0x02692DF0, GeckoU.Mix(0x38600000, PotionAmplifierSlider.Value));
        }
        private void DisableVPadInputToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x011293D0, Blr, 0x38E00001, disableVPadInput.Checked);
        }
        private void VpadDisplaySwitchToggled(object sender, EventArgs e)
        {
            //GeckoU.RpcToggle(0x0112A9E4, 0x0112A9EC, 0x0, 0x0, vpadDisplaySwitch.Checked);
        }

        private async void DebugConsoleToggled(object sender, EventArgs e)
        {
            uint[] bools = { 0x01, 0x00 };

            //GeckoU.RpcToggle(0x02DABA0C, 0x109F95E0, bools, DebugConsole.Checked, 0, 1);

            DebugConsole.Enabled = false;
            await PutTaskDelay(1000);
            DebugConsole.Enabled = true;
        }

        private void UnlockSignKeyboardToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02F88110, 0x39400002, 0x39400003, UnlockSignKeyboard.Checked);
            ////GeckoU.WriteUIntToggle(0x02FAF4F0, 0x39400002, 0x39400003, UnlockSignKeyboard.Checked);
            ////GeckoU.WriteUIntToggle(0x02FAF560, 0x39400002, 0x39400003, UnlockSignKeyboard.Checked);
            ////GeckoU.WriteUIntToggle(0x02FAF5DC, 0x39400002, 0x39400003, UnlockSignKeyboard.Checked);
            ////GeckoU.WriteUIntToggle(0x02FAF64C, 0x39400002, 0x39400003, UnlockSignKeyboard.Checked);
        }

        #region minigames
        private void AllPermissionsToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02C57E94, On, 0x57E3063E, AllPermissions.Checked); //MiniGameDef::MayUseBlock(const(int))
            ////GeckoU.WriteUIntToggle(0x02C57E34, On, 0x57E3063E, AllPermissions.Checked); //MiniGameDef::MayUse(const(int))
            ////GeckoU.WriteUIntToggle(0x02C51C20, On, 0x57E3063E, AllPermissions.Checked); //MiniGameDef::MayUseBlock(const(int))
            ////GeckoU.WriteUIntToggle(0x02C5CC84, On, 0X88630124, AllPermissions.Checked); //MiniGameDef::CanCraft(const(void))
            ////GeckoU.WriteUIntToggle(0x02C57D74, On, 0x57E3063E, AllPermissions.Checked); //MiniGameDef::MayPlace(const(int))
            ////GeckoU.WriteUIntToggle(0x02C57DD4, On, 0x57E3063E, AllPermissions.Checked); //MiniGameDef::MayDestroy(const(int))
        }

        private void AlwaysDamagedToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02C7DDD0, On, Off, AlwaysDamaged.Checked); //MasterGameMode::IsInKillBox((boost::shared_ptr__tm__8_6Entity))
        }

        private void TntGriefingToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02C59E04, On, Off, TNTGriefing.Checked); //MiniGameDef::canTntDestroyBlocks(const(void))
        }

        private void DisabledKillBarriersToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02C7DD38, Off, On, DisabledKillBarriers.Checked); //MasterGameMode::IsInKillBox((boost::shared_ptr__tm__8_6Entity))
        }

        private void AntiEndGameToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02C9554C, Off, On, AntiEndGame.Checked); //MasterGameMode::CanEndPostGame((void))
        }

        private void EndGameClicked(object sender, EventArgs e)
        {
            //GeckoU.WriteUInt(0x02C93A90, Blr);
            //GeckoU.WriteUInt(0x02C93A90, Mflr);
        }

        private void TumbleHudToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x02E3A950, Off, 0x57E3063E, TumbleHUD.Checked); //UIScene_HUD::HideMainHUD((void))
            ////GeckoU.WriteUIntToggle(0x02C57EBC, Off, 0x8863012A, TumbleHUD.Checked); //MiniGameDef::LockedInventory(const(void))
        }
        private void RequiredPlayersSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteUInt(0x02C7C004, GeckoU.Mix(0x38600000, RequiredPlayersSlider.Value)); //MasterGameMode::GetMinimumPlayersToStart
        }

        private void RefillIntervalSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteUInt(0x02C5CC74, GeckoU.Mix(0x38600000, RefillIntervalSlider.Value)); //MiniGameDef::GetItemRespawnTime
        }

        private void GlideRingScoreSliderChanged(object sender, EventArgs e)
        {
            //GeckoU.WriteUInt(0x02D6EE14, GeckoU.Mix(0x38600000, ringScoreGreen.Value)); //ScoreRingRuleDefinition::getPointValue((void))
            //GeckoU.WriteUInt(0x02D6EE1C, GeckoU.Mix(0x38600000, ringScoreBlue.Value));
            //GeckoU.WriteUInt(0x02D6EE24, GeckoU.Mix(0x38600000, ringScoreOrange.Value));
        }

        private void SqueakInfinitelyToggled(object sender, EventArgs e)
        {
            ////GeckoU.WriteUIntToggle(0x031A2730, Blr, Mflr, SqueakInfinitely.Checked); //MultiPlayerGameMode::CanMakeSpectateSound((void))
        }
        #endregion

        #endregion memory editing
    }
}
