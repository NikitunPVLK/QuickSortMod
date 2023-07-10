using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("QuickSortMod has landed");

            CreateConfigValues();
            AddInputs();
            AddLocalizations();
        }


        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;
            InventorySortSpecialConfig = Config.Bind("Client config", "Inventory Sorting", KeyCode.Home, new ConfigDescription("Key to sort your inventory"));
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
        }

        private void AddLocalizations()
        {
            Localization = new CustomLocalization();
            LocalizationManager.Instance.AddLocalization(Localization);

            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"$inventorysort_inventorysort", "Inventory sorted"}
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
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$inventorysort_inventorysort");
                        Sort();
                    }
                }
            }
        }
            
        private void Sort()
        {
            Inventory inventory = Player.m_localPlayer.GetInventory();
            List<ComparableItem> items = new List<ComparableItem>();
            
            for (int y = 1; y < inventory.m_height; y++)
            {
                for (int x = 0; x < inventory.m_width; x++)
                {
                    ItemDrop.ItemData item = inventory.GetItemAt(x, y);
                    if (item != null)
                    {
                        if (!item.m_equipped)
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
    }
}

