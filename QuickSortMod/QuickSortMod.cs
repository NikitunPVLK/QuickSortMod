using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QuickSortMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    internal class QuickSortMod : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.nikitunpvlk.quicksortmod";
        public const string PluginName = "QuickSortMod";
        public const string PluginVersion = "0.0.1";

        // Localization
        private CustomLocalization Localization;

        private ConfigEntry<KeyCode> InventorySortSpecialConfig;
        private ButtonConfig InventorySortSpecialButton;

        private ConfigEntry<KeyCode> QuickStackSpecialConfig;
        private ButtonConfig QuickStackSpecialButton;

        public static Container currentContainer;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("QuickSortMod has landed");

            CreateConfigValues();
            AddInputs();
            AddLocalizations();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;
            InventorySortSpecialConfig = Config.Bind("Client_config", "Inventory Sorting", KeyCode.R, new ConfigDescription("Key to sort your inventory"));
            QuickStackSpecialConfig = Config.Bind("Client_config", "Quick Stack", KeyCode.T, new ConfigDescription("Key to quick stack items to container"));
        }

        private void AddInputs()
        {
            InventorySortSpecialButton = new ButtonConfig
            {
                Name = "QuickSortMod_InventorySort",
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
                Config = InventorySortSpecialConfig,
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, InventorySortSpecialButton);

            QuickStackSpecialButton = new ButtonConfig
            {
                Name = "QuickSortMod_QuickStack",
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
                Config = QuickStackSpecialConfig,
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, QuickStackSpecialButton);
        }

        private void AddLocalizations()
        {
            Localization = new CustomLocalization();
            LocalizationManager.Instance.AddLocalization(Localization);

            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"$quicksort_inventorysort", "Inventory sorted"},
                {"$quicksort_quickstack", "Items stacked" }
            });
        }

        private void Update()
        {

            if (ZInput.instance != null)
            {
                if (InventorySortSpecialButton != null && MessageHud.instance != null && Player.m_localPlayer != null && InventoryGui.IsVisible())
                {
                    if (ZInput.GetButtonDown(InventorySortSpecialButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$quicksort_inventorysort");
                        Sort(Player.m_localPlayer.GetInventory(), 1);

                        if (currentContainer != null && currentContainer.GetInventory() != null && currentContainer.IsInUse())
                        {
                            Sort(currentContainer.GetInventory(), 0);
                        }
                    }
                }
                
                
                if (QuickStackSpecialButton != null && MessageHud.instance != null && Player.m_localPlayer != null && InventoryGui.IsVisible())
                {
                    if (ZInput.GetButtonDown(QuickStackSpecialButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        if (currentContainer != null && currentContainer.GetInventory() != null && currentContainer.IsInUse())
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$quicksort_quickstack");
                            StackItems();
                        }
                    }
                }
                
            }
        }

        private void Sort(Inventory inventory, int startY) 
        {
            List<ComparableItem> items = new List<ComparableItem>();

            for (int y = startY; y < inventory.m_height; y++)
            {
                for (int x = 0; x < inventory.m_width; x++)
                {
                    ItemDrop.ItemData item = inventory.GetItemAt(x, y);
                    if (item != null)
                    {
                        if (!item.IsEquipable() && item.m_shared.m_food == 0)
                        {
                            items.Add(new ComparableItem(item));
                            inventory.RemoveItem(item);
                        }
                    }
                }
            }

            items.Sort();

            foreach (ComparableItem item in items)
            {
                inventory.AddItem(item.item);
            }
        }

        private void StackItems()
        {
            Inventory containerInventory = currentContainer.GetInventory();
            Inventory playerInventory = Player.m_localPlayer.GetInventory();
            List<ItemDrop.ItemData> itemsToStack = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData containerItem in containerInventory.m_inventory)
            {
                String itemName = containerItem.m_shared.m_name;
                foreach (ItemDrop.ItemData playerItem in playerInventory.m_inventory)
                {
                    if (itemName.Equals(playerItem.m_shared.m_name) && !playerItem.IsEquipable() && playerItem.m_shared.m_food == 0)
                    {
                        itemsToStack.Add(playerItem);
                    }
                }
                
            }
            foreach (ItemDrop.ItemData item in itemsToStack)
            {
                if(containerInventory.CanAddItem(item) && containerInventory.AddItem(item))
                {
                    playerInventory.RemoveItem(item);
                }
            }
        }

        private class ComparableItem : IComparable<ComparableItem>
        {
            public ItemDrop.ItemData item;

            public ComparableItem(ItemDrop.ItemData item)
            {
                this.item = item;
            }

            public int CompareTo(ComparableItem otherItem)
            {
                return item.m_shared.m_name.CompareTo(otherItem.item.m_shared.m_name);
            }
        }

        [HarmonyPatch(typeof(Container), "RPC_OpenRespons")]
        static class Container_RPC_OpenRespons_Patch
        {
            private static void Postfix(Container __instance, ZNetView ___m_nview)
            {
                currentContainer = __instance;
            }
        }
    }
}

