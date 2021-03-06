﻿using System.Collections.Generic;
using BlockTypes.Builtin;

namespace Pandaros.Settlers.Cosmetics
{
    [ModLoader.ModManagerAttribute]
    public static class CarpetLime
    {
        private const string KEY = "carpetlime";

        public static string TEXTURE_KEY = GameLoader.NAMESPACE + "." + KEY;
        public static string NAME = GameLoader.NAMESPACE + "." + KEY;

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            GameLoader.NAMESPACE + ".Cosmetics." + KEY + ".AddTextures")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.registertexturemappingtextures")]
        public static void AddTextures()
        {
            Register.AddCarpetTextures(KEY);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes,
            GameLoader.NAMESPACE + ".Cosmetics." + KEY + ".AfterAddingBaseTypes")]
        public static void AfterAddingBaseTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
        {
            Item = Register.AddCarpetTypeTypes(itemTypes, KEY);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterWorldLoad,
            GameLoader.NAMESPACE + ".Cosmetics." + KEY + ".AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            var flax   = new InventoryItem(BuiltinBlocks.Flax, 1);
            var planks = new InventoryItem(BuiltinBlocks.Planks, 1);
            var linen  = new InventoryItem(BuiltinBlocks.Linen, 1);

            var recipe = new Recipe(NAME,
                                    new List<InventoryItem> {flax, planks, linen},
                                    new InventoryItem(Item.ItemIndex, 1), 2);

            //ItemTypesServer.LoadSortOrder(NAME, GameLoader.GetNextItemSortIndex());
            RecipeStorage.AddDefaultLimitTypeRecipe(Register.DYER_JOB, recipe);
        }
    }
}