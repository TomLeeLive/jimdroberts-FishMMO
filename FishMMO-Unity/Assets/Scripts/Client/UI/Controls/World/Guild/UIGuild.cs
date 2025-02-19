﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using FishMMO.Shared;

namespace FishMMO.Client
{
	public class UIGuild : UIControl
	{
		public int MaxGuildNameLength = 64;
		public TMP_Text GuildLabel;
		public RectTransform GuildMemberParent;
		public UIGuildMember GuildMemberPrefab;
		public Dictionary<long, UIGuildMember> Members = new Dictionary<long, UIGuildMember>();

		public override void OnStarting()
		{
		}

		public override void OnDestroying()
		{
			if (GuildLabel != null)
			{
				GuildLabel.text = "Guild";
			}
			foreach (UIGuildMember member in new List<UIGuildMember>(Members.Values))
			{
				Destroy(member.gameObject);
			}
			Members.Clear();
		}

		public void OnGuildCreated(string location)
		{
			Character character = Character.localCharacter;
			if (character != null && GuildMemberPrefab != null && GuildMemberParent != null)
			{
				ClientNamingSystem.SetName(NamingSystemType.GuildName, character.GuildController.ID, (s) =>
				{
					if (GuildLabel != null)
					{
						GuildLabel.text = s;
					}
				});
				
				UIGuildMember member = Instantiate(GuildMemberPrefab, GuildMemberParent);
				if (member != null)
				{
					if (member.Name != null)
						member.Name.text = character.CharacterName;
					if (member.Rank != null)
						member.Rank.text = character.GuildController.Rank.ToString();
					if (member.Location != null)
						member.Location.text = location;
					Members.Add(character.ID, member);
				}
			}
		}

		public void OnLeaveGuild()
		{
			if (GuildLabel != null)
			{
				GuildLabel.text = "Guild";
			}
			foreach (UIGuildMember member in new List<UIGuildMember>(Members.Values))
			{
				Destroy(member.gameObject);
			}
			Members.Clear();
		}

		public void OnGuildAddMember(long characterID, GuildRank rank, string location)
		{
			if (GuildMemberPrefab != null && GuildMemberParent != null)
			{
				if (!Members.TryGetValue(characterID, out UIGuildMember guildMember))
				{
					Members.Add(characterID, guildMember = Instantiate(GuildMemberPrefab, GuildMemberParent));
				}
				if (guildMember.Name != null)
				{
					ClientNamingSystem.SetName(NamingSystemType.CharacterName, characterID, (n) =>
					{
						guildMember.Name.text = n;
					});
				}
				if (guildMember.Rank != null)
					guildMember.Rank.text = rank.ToString();
				if (guildMember.Location != null)
					guildMember.Location.text = location;
			}
		}

		public void OnGuildRemoveMember(long characterID)
		{
			if (Members.TryGetValue(characterID, out UIGuildMember member))
			{
				Members.Remove(characterID);
				Destroy(member.gameObject);
			}
		}

		public void OnButtonCreateGuild()
		{
			Character character = Character.localCharacter;
			if (character != null && character.GuildController.ID < 1 && Client.NetworkManager.IsClient)
			{
				if (UIManager.TryGet("UIInputConfirmationTooltip", out UIInputConfirmationTooltip tooltip))
				{
					tooltip.Open("Please type the name of your new guild!", (s) =>
					{
						if (!string.IsNullOrWhiteSpace(s) &&
							s.Length <= MaxGuildNameLength &&
							Regex.IsMatch(s, @"^[A-Za-z]+(?: [A-Za-z]+){0,2}$"))
						{
							Client.NetworkManager.ClientManager.Broadcast(new GuildCreateBroadcast()
							{
								guildName = s,
							});
						}
					}, null);
				}
			}
		}

		public void OnButtonLeaveGuild()
		{
			Character character = Character.localCharacter;
			if (character != null && character.GuildController.ID > 0 && Client.NetworkManager.IsClient)
			{
				if (UIManager.TryGet("UIConfirmationTooltip", out UIConfirmationTooltip tooltip))
				{
					tooltip.Open("Are you sure you want to leave your guild?", () =>
					{
						Client.NetworkManager.ClientManager.Broadcast(new GuildLeaveBroadcast());
					}, null);
				}
			}
		}

		public void OnButtonInviteToGuild()
		{
			Character character = Character.localCharacter;
			if (character != null && character.GuildController.ID > 0 && Client.NetworkManager.IsClient)
			{
				if (character.TargetController.Current.Target != null)
				{
					Character targetCharacter = character.TargetController.Current.Target.GetComponent<Character>();
					if (targetCharacter != null)
					{
						Client.NetworkManager.ClientManager.Broadcast(new GuildInviteBroadcast()
						{
							targetCharacterID = targetCharacter.ID
						});
					}
				}
			}
		}

		public void OnClose()
		{
			Visible = false;
		}
	}
}