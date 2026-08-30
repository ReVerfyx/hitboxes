package com.local.hitboxdebug.config;

import com.local.hitboxdebug.feature.farmbuilder.Blueprints;

/**
 * In-memory toggle state for the debug panel. Intentionally not persisted
 * to disk with hidden defaults — every toggle starts OFF and is visible
 * in {@link com.local.hitboxdebug.gui.DebugPanelScreen}.
 */
public final class ModConfig {

	public boolean autoFarmMobsEnabled = false;
	public boolean hitboxVisualizerEnabled = false;
	public boolean autoEatEnabled = false;
	public boolean farmBuilderEnabled = false;
	public boolean hitboxExpandEnabled = false;

	/** Blocks added to each side of a mob's hit-detection box when hitbox
	 *  enlargement is on. Never applied to {@link net.minecraft.entity.player.PlayerEntity}. */
	public double hitboxExpandAmount = 0.25;

	/** Radius (blocks) auto-farm mobs will search for animals. */
	public double autoFarmRadius = 8.0;

	/** Radius (blocks) that must be free of other real players for any
	 *  automation feature to run. See {@link com.local.hitboxdebug.util.SafetyGuard}. */
	public double otherPlayerSafetyRadius = 48.0;

	public Blueprints.Type selectedBlueprint = Blueprints.Type.SIMPLE_CROP_FIELD;
}
