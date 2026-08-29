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

/** ReVerfyx Client control panel. Right Shift opens it. */
public final class DebugPanelScreen extends Screen {

    private final ModConfig config = HitboxDebugClient.CONFIG;

    public DebugPanelScreen() {
        super(new LiteralText("ReVerfyx Client"));
    }

    @Override
    protected void init() {
        int panelX = width / 2 - 205;
        int top = height / 2 - 105;
        int colGap = 12;
        int buttonWidth = 198;
        int left = panelX + 5;
        int right = left + buttonWidth + colGap;

        addButton(toggleButton(left, top, buttonWidth, "hitboxdebug.panel.autofarm_mobs",
                config.autoFarmMobsEnabled, v -> config.autoFarmMobsEnabled = v));
        addButton(toggleButton(right, top, buttonWidth, "hitboxdebug.panel.hitbox_visualizer",
                config.hitboxVisualizerEnabled, v -> config.hitboxVisualizerEnabled = v));

        addButton(toggleButton(left, top + 28, buttonWidth, "hitboxdebug.panel.hitbox_expand",
                config.hitboxExpandEnabled, v -> config.hitboxExpandEnabled = v));
        addButton(toggleButton(right, top + 28, buttonWidth, "hitboxdebug.panel.autoeat",
                config.autoEatEnabled, v -> config.autoEatEnabled = v));

        addButton(blueprintCycleButton(left, top + 56, buttonWidth));
        addButton(new ButtonWidget(right, top + 56, buttonWidth, 20,
                new TranslatableText("hitboxdebug.panel.farmbuilder.start"),
                button -> startFarmBuilderAtPlayer()));

        addButton(new ButtonWidget(left, top + 84, buttonWidth, 20,
                new TranslatableText("hitboxdebug.panel.farmbuilder.stop"),
                button -> {
                    config.farmBuilderEnabled = false;
                    HitboxDebugClient.FARM_BUILDER.stop();
                }));

        addButton(new ButtonWidget(right, top + 84, buttonWidth, 20,
                new LiteralText("Закрыть"), button -> onClose()));
    }

    private void startFarmBuilderAtPlayer() {
        if (client == null || client.player == null) return;
        BlockPos origin = client.player.getBlockPos();
        HitboxDebugClient.FARM_BUILDER.start(config.selectedBlueprint, origin);
        config.farmBuilderEnabled = true;
    }

    private ButtonWidget toggleButton(int x, int y, int width, String key, boolean initial,
                                      java.util.function.Consumer<Boolean> onToggle) {
        boolean[] state = {initial};
        ButtonWidget[] self = new ButtonWidget[1];
        self[0] = new ButtonWidget(x, y, width, 20, toggleLabel(key, state[0]), button -> {
            state[0] = !state[0];
            onToggle.accept(state[0]);
            self[0].setMessage(toggleLabel(key, state[0]));
        });
        return self[0];
    }

    private Text toggleLabel(String key, boolean value) {
        return new TranslatableText(key).append(new LiteralText(value ? "  •  ON" : "  •  OFF"));
    }

    private ButtonWidget blueprintCycleButton(int x, int y, int width) {
        ButtonWidget[] self = new ButtonWidget[1];
        self[0] = new ButtonWidget(x, y, width, 20, blueprintLabel(), button -> {
            Blueprints.Type[] values = Blueprints.Type.values();
            int nextIndex = (config.selectedBlueprint.ordinal() + 1) % values.length;
            config.selectedBlueprint = values[nextIndex];
            self[0].setMessage(blueprintLabel());
        });
        return self[0];
    }

    private Text blueprintLabel() {
        return new LiteralText("Чертёж: " + Blueprints.displayNames().get(config.selectedBlueprint));
    }

    private static final int PANEL = 0xEA0C0E14;
    private static final int PANEL_INNER = 0xCC151923;
    private static final int BORDER = 0xFF8B7CFF;
    private static final int TEXT_MUTED = 0xFF9B9EAA;

    @Override
    public void render(MatrixStack matrices, int mouseX, int mouseY, float delta) {
        renderBackground(matrices);

        int x1 = width / 2 - 210;
        int y1 = height / 2 - 135;
        int x2 = width / 2 + 210;
        int y2 = height / 2 + 112;

        // Layered rectangles imitate a compact glass card on 1.16.5, where
        // the GUI API has no native backdrop blur.
        fill(matrices, x1 - 2, y1 - 2, x2 + 2, y2 + 2, BORDER);
        fill(matrices, x1, y1, x2, y2, PANEL);
        fill(matrices, x1 + 5, y1 + 5, x2 - 5, y2 - 5, PANEL_INNER);

        super.render(matrices, mouseX, mouseY, delta);

        drawCenteredText(matrices, textRenderer, title, width / 2, y1 + 16, 0xFFFFFF);
        drawCenteredText(matrices, textRenderer, new LiteralText("Right Shift  ·  local utility panel"),
                width / 2, y1 + 32, TEXT_MUTED);
        drawCenteredText(matrices, textRenderer,
                new TranslatableText("hitboxdebug.panel.warning"),
                width / 2, y2 - 12, 0xFFB7A8FF);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
