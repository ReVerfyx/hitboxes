package com.local.hitboxdebug.gui;

import com.local.hitboxdebug.HitboxDebugClient;
import com.local.hitboxdebug.config.ModConfig;
import com.local.hitboxdebug.feature.farmbuilder.Blueprints;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.text.LiteralText;
import net.minecraft.text.Text;
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

		addDrawableChild(blueprintCycleButton(x, y + spacing * 3));

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

	// 1.16.5 has no CyclingButtonWidget (that's a 1.17+ addition) — plain
	// ButtonWidget with the on/off state baked into its own label instead.
	private ButtonWidget toggleButton(int x, int y, String key, boolean initial,
			java.util.function.Consumer<Boolean> onToggle) {
		boolean[] state = {initial};
		ButtonWidget[] self = new ButtonWidget[1];
		self[0] = new ButtonWidget(x, y, 200, 20, toggleLabel(key, state[0]), button -> {
			state[0] = !state[0];
			onToggle.accept(state[0]);
			self[0].setMessage(toggleLabel(key, state[0]));
		});
		return self[0];
	}

	private Text toggleLabel(String key, boolean value) {
		return new TranslatableText(key).append(new LiteralText(value ? " [ON]" : " [OFF]"));
	}

	private ButtonWidget blueprintCycleButton(int x, int y) {
		ButtonWidget[] self = new ButtonWidget[1];
		self[0] = new ButtonWidget(x, y, 200, 20, blueprintLabel(), button -> {
			Blueprints.Type[] values = Blueprints.Type.values();
			int nextIndex = (config.selectedBlueprint.ordinal() + 1) % values.length;
			config.selectedBlueprint = values[nextIndex];
			self[0].setMessage(blueprintLabel());
		});
		return self[0];
	}

	private Text blueprintLabel() {
		return new LiteralText(Blueprints.displayNames().get(config.selectedBlueprint));
	}

	// Same glass-panel language as the ReVerfyx Client Launcher: a
	// translucent dark card with a thin accent-colored border. Minecraft's
	// 1.16.5 GUI stack has no backdrop blur to draw on, so this is a flat
	// tinted rectangle rather than a true blur — same idea, GL-simple version.
	private static final int GLASS_BORDER_COLOR = 0x804FA8FF;
	private static final int GLASS_FILL_COLOR = 0xB0141A24;

	@Override
	public void render(MatrixStack matrices, int mouseX, int mouseY, float delta) {
		renderBackground(matrices);
		renderGlassPanel(matrices);
		super.render(matrices, mouseX, mouseY, delta);
		drawCenteredText(matrices, textRenderer, title, width / 2, height / 2 - 110, 0xFFFFFF);
		drawCenteredText(matrices, textRenderer,
				new TranslatableText("hitboxdebug.panel.warning"),
				width / 2, height / 2 + 70, 0xFFAA00);
	}

	private void renderGlassPanel(MatrixStack matrices) {
		int x1 = width / 2 - 122;
		int y1 = height / 2 - 130;
		int x2 = width / 2 + 122;
		int y2 = height / 2 + 100;

		fill(matrices, x1 - 2, y1 - 2, x2 + 2, y2 + 2, GLASS_BORDER_COLOR);
		fill(matrices, x1, y1, x2, y2, GLASS_FILL_COLOR);
	}

	@Override
	public boolean isPauseScreen() {
		return false;
	}
}
