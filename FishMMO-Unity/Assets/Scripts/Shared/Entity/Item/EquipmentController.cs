﻿using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

namespace FishMMO.Shared
{
	[RequireComponent(typeof(Character))]
	public class EquipmentController : ItemContainer
	{
		public Character Character;

		private void Awake()
		{
			AddSlots(null, System.Enum.GetNames(typeof(ItemSlot)).Length); // equipment size = itemslot size
		}

#if !UNITY_SERVER
		public override void OnStartClient()
		{
			base.OnStartClient();

			if (!base.IsOwner)
			{
				enabled = false;
				return;
			}

			ClientManager.RegisterBroadcast<EquipmentSetItemBroadcast>(OnClientEquipmentSetItemBroadcastReceived);
			ClientManager.RegisterBroadcast<EquipmentSetMultipleItemsBroadcast>(OnClientEquipmentSetMultipleItemsBroadcastReceived);
			ClientManager.RegisterBroadcast<EquipmentEquipItemBroadcast>(OnClientEquipmentEquipItemBroadcastReceived);
			ClientManager.RegisterBroadcast<EquipmentUnequipItemBroadcast>(OnClientEquipmentUnequipItemBroadcastReceived);
		}

		public override void OnStopClient()
		{
			base.OnStopClient();

			if (base.IsOwner)
			{
				ClientManager.UnregisterBroadcast<EquipmentSetItemBroadcast>(OnClientEquipmentSetItemBroadcastReceived);
				ClientManager.UnregisterBroadcast<EquipmentSetMultipleItemsBroadcast>(OnClientEquipmentSetMultipleItemsBroadcastReceived);
				ClientManager.UnregisterBroadcast<EquipmentEquipItemBroadcast>(OnClientEquipmentEquipItemBroadcastReceived);
				ClientManager.UnregisterBroadcast<EquipmentUnequipItemBroadcast>(OnClientEquipmentUnequipItemBroadcastReceived);
			}
		}

		/// <summary>
		/// Server sent a set item broadcast. Item slot is set to the received item details.
		/// </summary>
		private void OnClientEquipmentSetItemBroadcastReceived(EquipmentSetItemBroadcast msg)
		{
			Item newItem = new Item(msg.instanceID, msg.templateID, msg.stackSize);
			Equip(newItem, -1, (ItemSlot)msg.slot);
		}

		/// <summary>
		/// Server sent a multiple set item broadcast. Item slot is set to the received item details.
		/// </summary>
		private void OnClientEquipmentSetMultipleItemsBroadcastReceived(EquipmentSetMultipleItemsBroadcast msg)
		{
			foreach (EquipmentSetItemBroadcast subMsg in msg.items)
			{
				Item newItem = new Item(subMsg.instanceID, subMsg.templateID, subMsg.stackSize);
				Equip(newItem, -1, (ItemSlot)subMsg.slot);
			}
		}

		/// <summary>
		/// Server sent an equip item broadcast.
		/// </summary>
		private void OnClientEquipmentEquipItemBroadcastReceived(EquipmentEquipItemBroadcast msg)
		{
			if (Character.InventoryController.TryGetItem(msg.inventoryIndex, out Item item))
			{
				Equip(item, msg.inventoryIndex, (ItemSlot)msg.slot);
			}
		}

		/// <summary>
		/// Server sent an unequip item broadcast.
		/// </summary>
		private void OnClientEquipmentUnequipItemBroadcastReceived(EquipmentUnequipItemBroadcast msg)
		{
			if (Character.InventoryController == null)
			{
				return;
			}
			Unequip(Character.InventoryController, msg.slot);
		}
#endif

		public void SendEquipRequest(int inventoryIndex, byte slot)
		{
			ClientManager.Broadcast(new EquipmentEquipItemBroadcast()
			{
				inventoryIndex = inventoryIndex,
				slot = slot,
			}, Channel.Reliable);
		}

		public void SendUnequipRequest(byte slot)
		{
			ClientManager.Broadcast(new EquipmentUnequipItemBroadcast()
			{
				slot = slot,
			}, Channel.Reliable);
		}

		public override bool CanManipulate()
		{
			if (!base.CanManipulate())
			{
				return false;
			}

			/*if ((character.State == CharacterState.Idle ||
				  character.State == CharacterState.Moving) &&
				  character.State != CharacterState.UsingObject &&
				  character.State != CharacterState.IsFrozen &&
				  character.State != CharacterState.IsStunned &&
				  character.State != CharacterState.IsMesmerized) return true;
			*/
			return true;
		}

		public void Activate(int index)
		{
			if (TryGetItem(index, out Item item))
			{
				Debug.Log("EquipmentController: using item in slot[" + index + "]");
				//items[index].OnUseItem();
			}
		}

		public bool Equip(Item item, int inventoryIndex, ItemSlot slot)
		{
			if (item == null || !CanManipulate()) return false;

			EquippableItemTemplate Equippable = item.Template as EquippableItemTemplate;
			// make sure the slot type matches so we aren't equipping things in weird places
			if (Equippable == null || slot != Equippable.Slot)
			{
				return false;
			}

			byte slotIndex = (byte)slot;
			Item prevItem = Items[slotIndex];
			if (prevItem != null &&
				prevItem.Equippable != null)
			{
				prevItem.Equippable.Unequip();

				// swap the items
				Character.InventoryController.SetItemSlot(prevItem, inventoryIndex);
			}
			else
			{
				// remove the item from the inventory
				Character.InventoryController.RemoveItem(inventoryIndex);
			}

			// put the new item in the correct slot
			if (!SetItemSlot(item, slotIndex))
			{
				return false;
			}

			// equip the item to the character (adds attributes.. etc..)
			if (item.Equippable != null)
			{
				item.Equippable.Equip(Character);
			}
			return true;
		}

		/// <summary>
		/// Unequips the item and puts it in the inventory.
		/// </summary>
		public bool Unequip(ItemContainer container, byte slot)
		{
			if (!CanManipulate() ||
				!TryGetItem(slot, out Item item) ||
				!Character.InventoryController.CanAddItem(item))
			{
				return false;
			}

			// remove the equipped item
			SetItemSlot(null, slot);

			// unequip the item
			if (item.Equippable != null)
			{
				item.Equippable.Unequip();
			}

			// try to add the item back to the inventory
			container.TryAddItem(item, out List<Item> modifiedItems);

			return true;
		}
	}
}