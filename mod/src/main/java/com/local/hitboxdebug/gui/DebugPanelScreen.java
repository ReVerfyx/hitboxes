package com.local.hitboxdebug.gui;

import com.local.hitboxdebug.HitboxDebugClient;
import com.local.hitboxdebug.config.ModConfig;
import com.local.hitboxdebug.feature.farmbuilder.Blueprints;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.client.gui.widget.CyclingButtonWidget;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.text.LiteralText;
import net.minecraft.text.TranslatableText;
import net.minecraft.util.math.BlockPos;

/**
 * The Right-Shift panel. Every toggle here is a plain, always-visible
 * checkbox-style button — nothing is hidden, nothing auto-enables, and the
 * "singleplayer / local world only" notice is a static label (not a
 * dismissible popup) so it stays in view whenever the panel is open.
 */
public final class DebugPanelScreen extends Screen {

	private final ModConfig config = HitboxDebugClient.CONFIG;

	private BlockPos pendingCorner;

	public DebugPanelScreen() {
		super(new TranslatableText("hitboxdebug.panel.title"));
	}

	@Override
	protected void init() {
		int x = width / 2 - 100;
		int y = height / 2 - 90;
		int spacing = 24;

		addDrawableChild(toggleButton(x, y, "hitboxdebug.panel.autofarm_mobs",
				config.autoFarmMobsEnabled, v -> config.autoFarmMobsEnabled = v));

		addDrawableChild(toggleButton(x, y + spacing, "hitboxdebug.panel.hitbox_visualizer",
				config.hitboxVisualizerEnabled, v -> config.hitboxVisualizerEnabled = v));

		addDrawableChild(toggleButton(x, y + spacing * 2, "hitboxdebug.panel.autoeat",
				config.autoEatEnabled, v -> config.autoEatEnabled = v));

		addDrawableChild(CyclingButtonWidget.<Blueprints.Type>builder(
						type -> new LiteralText(Blueprints.displayNames().get(type)))
				.values(Blueprints.Type.values())
				.initially(config.selectedBlueprint)
				.build(x, y + spacing * 3, 200, 20,
						new TranslatableText("hitboxdebug.panel.farmbuilder.select"),
						(button, value) -> config.selectedBlueprint = value));

		addDrawableChild(new ButtonWidget(x, y + spacing * 4, 200, 20,
				new TranslatableText("hitboxdebug.panel.farmbuilder.start"),
				button -> startFarmBuilderAtPlayer()));

		addDrawableChild(new ButtonWidget(x, y + spacing * 5, 200, 20,
				new TranslatableText("hitboxdebug.panel.farmbuilder.stop"),
				button -> {
					config.farmBuilderEnabled = false;
					HitboxDebugClient.FARM_BUILDER.stop();
				}));

		addDrawableChild(toggleButton(x, y + spacing * 6, "hitboxdebug.panel.hitbox_expand",
				config.hitboxExpandEnabled, v -> config.hitboxExpandEnabled = v));

		addDrawableChild(new ButtonWidget(x, y + spacing * 7, 200, 20,
				new LiteralText("Close"), button -> close()));
	}

	private void startFarmBuilderAtPlayer() {
		if (client == null || client.player == null) {
			return;
		}
		BlockPos origin = client.player.getBlockPos();
		HitboxDebugClient.FARM_BUILDER.start(config.selectedBlueprint, origin);
		config.farmBuilderEnabled = true;
	}

	private ButtonWidget toggleButton(int x, int y, String key, boolean initial,
			java.util.function.Consumer<Boolean> onToggle) {
		return CyclingButtonWidget.onOffBuilder(initial)
				.build(x, y, 200, 20, new TranslatableText(key),
						(button, value) -> onToggle.accept(value));
	}

	@Override
	public void render(MatrixStack matrices, int mouseX, int mouseY, float delta) {
		renderBackground(matrices);
		super.render(matrices, mouseX, mouseY, delta);
		drawCenteredText(matrices, textRenderer, title, width / 2, height / 2 - 110, 0xFFFFFF);
		drawCenteredText(matrices, textRenderer,
				new TranslatableText("hitboxdebug.panel.warning"),
				width / 2, height / 2 + 70, 0xFFAA00);
	}

	@Override
	public boolean isPauseScreen() {
		return false;
	}
}
