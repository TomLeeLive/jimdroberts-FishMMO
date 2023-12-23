﻿using System.Linq;
using FishMMO.Database.Npgsql;
using FishMMO.Database.Npgsql.Entities;
using FishMMO.Shared;

namespace FishMMO.Server.DatabaseServices
{
	public class CharacterInventoryService
	{
		/// <summary>
		/// Updates a CharacterInventoryItem slot to new values or adds a new CharacterInventoryItem and initializes the Item with the new ID.
		/// </summary>
		public static void UpdateOrAdd(NpgsqlDbContext dbContext, long characterID, Item item)
		{
			if (item == null)
			{
				return;
			}

			var dbItem = dbContext.CharacterInventoryItems.FirstOrDefault(c => c.CharacterID == characterID && c.ID == item.ID);
			// update slot or add
			if (dbItem != null)
			{
				dbItem.CharacterID = characterID;
				dbItem.TemplateID = item.Template.ID;
				dbItem.Slot = item.Slot;
				dbItem.Amount = item.IsStackable ? item.Stackable.Amount : 0;
			}
			else
			{
				dbItem = new CharacterInventoryEntity()
				{
					CharacterID = characterID,
					TemplateID = item.Template.ID,
					Slot = item.Slot,
					Amount = item.IsStackable ? item.Stackable.Amount : 0,
				};
				dbContext.CharacterInventoryItems.Add(dbItem);
				dbContext.SaveChanges();
				item.Initialize(dbItem.ID);
			}
		}

		/// <summary>
		/// Save a characters inventory to the database.
		/// </summary>
		public static void Save(NpgsqlDbContext dbContext, Character character)
		{
			if (character == null)
			{
				return;
			}

			var dbInventoryItems = dbContext.CharacterInventoryItems.Where(c => c.CharacterID == character.ID)
																	.ToDictionary(k => k.Slot);

			foreach (Item item in character.InventoryController.Items)
			{
				if (dbInventoryItems.TryGetValue(item.Slot, out CharacterInventoryEntity dbItem))
				{
					dbItem.CharacterID = character.ID;
					dbItem.TemplateID = item.Template.ID;
					dbItem.Slot = item.Slot;
					dbItem.Amount = item.IsStackable ? item.Stackable.Amount : 0;
				}
				else
				{
					dbContext.CharacterInventoryItems.Add(new CharacterInventoryEntity()
					{
						CharacterID = character.ID,
						TemplateID = item.Template.ID,
						Slot = item.Slot,
						Amount = item.IsStackable ? item.Stackable.Amount : 0,
					});
				}
			}
		}

		/// <summary>
		/// KeepData is automatically false... This means we delete the item. TODO Deleted field is simply set to true just incase we need to reinstate a character..
		/// </summary>
		public static void Delete(NpgsqlDbContext dbContext, long characterID, bool keepData = false)
		{
			if (!keepData)
			{
				var dbInventoryItems = dbContext.CharacterInventoryItems.Where(c => c.CharacterID == characterID);
				if (dbInventoryItems != null)
				{
					dbContext.CharacterInventoryItems.RemoveRange(dbInventoryItems);
				}
			}
		}

		/// <summary>
		/// KeepData is automatically false... This means we delete the item. TODO Deleted field is simply set to true just incase we need to reinstate a character..
		/// </summary>
		public static void Delete(NpgsqlDbContext dbContext, long characterID, long itemID, bool keepData = false)
		{
			if (!keepData)
			{
				var dbItem = dbContext.CharacterInventoryItems.FirstOrDefault(c => c.CharacterID == characterID && c.ID == itemID);
				if (dbItem != null)
				{
					dbContext.CharacterInventoryItems.Remove(dbItem);
				}
			}
		}

		/// <summary>
		/// Load character inventory from the database.
		/// </summary>
		public static void Load(NpgsqlDbContext dbContext, Character character)
		{
			var dbInventoryItems = dbContext.CharacterInventoryItems.Where(c => c.CharacterID == character.ID);
			foreach (CharacterInventoryEntity dbItem in dbInventoryItems)
			{
				BaseItemTemplate template = BaseItemTemplate.Get<BaseItemTemplate>(dbItem.TemplateID);
				if (template == null)
				{
					return;
				}
				Item item = new Item(dbItem.ID, template, dbItem.Amount);
				if (item == null)
				{
					return;
				}
				character.InventoryController.SetItemSlot(item, dbItem.Slot);
			};
		}
	}
}