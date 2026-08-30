package com.local.hitboxdebug.feature;

import com.local.hitboxdebug.HitboxDebugClient;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.item.ItemStack;
import net.minecraft.item.Items;
import net.minecraft.util.Hand;

/**
 * Eats a golden apple / enchanted golden apple from the hotbar once hunger
 * drops below a threshold, or immediately if the player is on low health
 * (for the enchanted apple's regeneration). Pure QoL — affects only the
 * local player's own hunger/health.
 */
public final class AutoEat {

	private static final int HUNGER_THRESHOLD = 17; // out of 20
	private static final float LOW_HEALTH_THRESHOLD = 10.0f; // out of 20
	private int useTicks = 0;

	public void tick(MinecraftClient client) {
		if (!HitboxDebugClient.CONFIG.autoEatEnabled) {
			return;
		}

		ClientPlayerEntity player = client.player;
		if (player == null) {
			return;
		}

		if (player.isUsingItem()) {
			return;
		}

		boolean shouldEat = player.getHungerManager().getFoodLevel() < HUNGER_THRESHOLD
				|| player.getHealth() < LOW_HEALTH_THRESHOLD;
		if (!shouldEat) {
			return;
		}

		Hand hand = findAppleHand(player);
		if (hand == null) {
			return;
		}

		client.options.useKey.setPressed(true);
		player.setCurrentHand(hand);
	}

	private Hand findAppleHand(ClientPlayerEntity player) {
		for (Hand hand : Hand.values()) {
			ItemStack stack = player.getStackInHand(hand);
			if (stack.getItem() == Items.GOLDEN_APPLE || stack.getItem() == Items.ENCHANTED_GOLDEN_APPLE) {
				return hand;
			}
		}
		return null;
	}
}
