package com.local.hitboxdebug;

import com.local.hitboxdebug.config.ModConfig;
import com.local.hitboxdebug.feature.AutoEat;
import com.local.hitboxdebug.feature.AutoFarmMobs;
import com.local.hitboxdebug.feature.HitboxVisualizer;
import com.local.hitboxdebug.feature.farmbuilder.AutoFarmBuilder;
import com.local.hitboxdebug.gui.DebugPanelScreen;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.keybinding.v1.KeyBindingHelper;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderEvents;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.KeyBinding;
import net.minecraft.client.util.InputUtil;
import org.lwjgl.glfw.GLFW;

/**
 * Client entrypoint. Everything here is a singleplayer/local-world debug
 * utility — no feature in this mod targets other players.
 */
public final class HitboxDebugClient implements ClientModInitializer {

	public static final ModConfig CONFIG = new ModConfig();

	public static final AutoFarmMobs AUTO_FARM_MOBS = new AutoFarmMobs();
	public static final HitboxVisualizer HITBOX_VISUALIZER = new HitboxVisualizer();
	public static final AutoEat AUTO_EAT = new AutoEat();
	public static final AutoFarmBuilder FARM_BUILDER = new AutoFarmBuilder();

	private static KeyBinding openPanelKey;

	@Override
	public void onInitializeClient() {
		openPanelKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
				"key.hitboxdebug.open_panel",
				InputUtil.Type.KEYSYM,
				GLFW.GLFW_KEY_RIGHT_SHIFT,
				"category.hitboxdebug"
		));

		ClientTickEvents.END_CLIENT_TICK.register(this::onClientTick);
		WorldRenderEvents.AFTER_TRANSLUCENT.register(HITBOX_VISUALIZER::render);
	}

	private void onClientTick(MinecraftClient client) {
		if (client.player == null || client.world == null) {
			return;
		}

		while (openPanelKey.wasPressed()) {
			if (client.currentScreen == null) {
				client.openScreen(new DebugPanelScreen());
			}
		}

		AUTO_FARM_MOBS.tick(client);
		AUTO_EAT.tick(client);
		FARM_BUILDER.tick(client);
	}
}
