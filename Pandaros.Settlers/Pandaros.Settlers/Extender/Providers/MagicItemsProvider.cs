﻿using Pandaros.Settlers.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pandaros.Settlers.Extender.Providers
{
    public class MagicItemsProvider : ISettlersExtension
    {
        public List<Type> LoadedAssembalies { get; } = new List<Type>();

        public string InterfaceName => nameof(IMagicItem);

        public void AfterAddingBaseTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
        {
            
        }

        public void AfterItemTypesDefined()
        {
            
        }

        public void AfterSelectedWorld()
        {
           
        }

        public void AfterWorldLoad()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("-------------------Magic Items Loaded----------------------");
            sb.AppendLine("");

            foreach (var item in LoadedAssembalies)
            {
                if (Activator.CreateInstance(item) is IMagicItem magicItem &&
                    !string.IsNullOrEmpty(magicItem.Name))
                {
                    sb.Append($"{magicItem.Name}, ");
                }
            }

            sb.AppendLine("");
            sb.AppendLine("---------------------------------------------------------");

            PandaLogger.Log(ChatColor.lime, sb.ToString());
        }
    }
}
