using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PauseKey.Common.Systems
{
	public class PauseSystem : ModSystem
	{
		public static ModKeybind PauseKeybind { get; private set; }
		public static bool Paused { get; private set; }

		public override void Load()
		{
			PauseKeybind = KeybindLoader.RegisterKeybind(Mod, "Pause", "P");
			On_Main.CanPauseGame += On_Main_CanPauseGame;
		}

		public override void Unload()
		{
			PauseKeybind = null;
			Paused = false;
		}

		private static bool On_Main_CanPauseGame(On_Main.orig_CanPauseGame orig)
		{
			return orig() || (Paused && Main.netMode == NetmodeID.SinglePlayer);
		}

		// Runs every frame, even while paused, so the key can also unpause.
		public override void PostUpdateInput()
		{
			if (Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer)
				return;

			// Otherwise the player would be stuck paused with no way to resume.
			if (Paused && PauseKeybind.GetAssignedKeys().Count == 0)
				Paused = false;

			if (Main.drawingPlayerChat || Main.editSign || Main.editChest
				|| PlayerInput.WritingText || Main.CurrentInputTextTakerOverride != null)
				return;

			if (PauseKeybind.JustPressed)
				Paused = !Paused;
		}

		public override void OnWorldUnload()
		{
			Paused = false;
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (index == -1)
				index = layers.Count;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PauseKey: Paused Message",
				DrawPausedMessage,
				InterfaceScaleType.UI));
		}

		private static bool DrawPausedMessage()
		{
			if (!Paused || Main.netMode != NetmodeID.SinglePlayer)
				return true;

			string key = PauseKeybind.GetAssignedKeys().FirstOrDefault()
				?? Language.GetTextValue("Mods.PauseKey.Unbound");
			string title = Language.GetTextValue("Mods.PauseKey.Paused");
			string subtitle = Language.GetTextValue("Mods.PauseKey.PressToUnpause", key);

			// Screen size is already in UI-scaled units inside UI layers.
			Vector2 center = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;

			var titleFont = FontAssets.DeathText.Value;
			var subFont = FontAssets.MouseText.Value;
			Vector2 titleSize = ChatManager.GetStringSize(titleFont, title, Vector2.One);
			Vector2 subSize = ChatManager.GetStringSize(subFont, subtitle, Vector2.One);

			const int padding = 20;
			const int gap = 4;
			int width = (int)Math.Max(titleSize.X, subSize.X) + padding * 2;
			int height = (int)(titleSize.Y + gap + subSize.Y) + padding * 2;
			var panel = new Rectangle(
				(int)(center.X - width / 2f),
				(int)(center.Y - height / 2f),
				width,
				height);
			Utils.DrawInvBG(Main.spriteBatch, panel);

			float y = panel.Y + padding;
			DrawCentered(titleFont, title, titleSize, center.X, y, Color.White);

			y += titleSize.Y + gap;
			DrawCentered(subFont, subtitle, subSize, center.X, y, Color.LightGray);

			return true;
		}

		private static void DrawCentered(DynamicSpriteFont font, string text,
			Vector2 size, float centerX, float y, Color color)
		{
			var position = new Vector2(centerX - size.X / 2f, y);
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text,
				position, color, 0f, Vector2.Zero, Vector2.One);
		}
	}
}
