package com.local.hitboxdebug.feature.farmbuilder;

import com.local.hitboxdebug.HitboxDebugClient;
import com.local.hitboxdebug.util.SafetyGuard;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.item.Item;
import net.minecraft.item.ItemStack;
import net.minecraft.text.TranslatableText;
import net.minecraft.util.Hand;
import net.minecraft.util.hit.BlockHitResult;
import net.minecraft.util.hit.HitResult;
import net.minecraft.util.math.BlockPos;
import net.minecraft.util.math.Direction;
import net.minecraft.util.math.Vec3d;

/**
 * Places a {@link FarmBlueprint}'s blocks one at a time, exactly like a
 * player would: each step waits for the client to be within normal
 * survival reach of the target position and for the required item to be
 * in the hotbar/inventory before "clicking" it in — no instant /fill,
 * no teleporting the player, no reaching further than vanilla allows.
 * Disabled whenever {@link SafetyGuard} sees another player nearby.
 */
public final class AutoFarmBuilder {

	private static final double REACH = 4.5;
	private static final int TICKS_BETWEEN_PLACEMENTS = 6;

	private FarmBlueprint activeBlueprint;
	private BlockPos origin;
	private int stepIndex;
	private int cooldown;
	private boolean waitingForMove;

	public void start(Blueprints.Type type, BlockPos originCorner) {
		this.activeBlueprint = Blueprints.get(type);
		this.origin = originCorner;
		this.stepIndex = 0;
		this.cooldown = 0;
		this.waitingForMove = false;
	}

	public void stop() {
		this.activeBlueprint = null;
	}

	public boolean isRunning() {
		return activeBlueprint != null;
	}

	public FarmBlueprint getActiveBlueprint() {
		return activeBlueprint;
	}

	public int getStepIndex() {
		return stepIndex;
	}

	public void tick(MinecraftClient client) {
		if (!HitboxDebugClient.CONFIG.farmBuilderEnabled || activeBlueprint == null) {
			return;
		}
		if (!SafetyGuard.canAutomate(client)) {
			return;
		}

		ClientPlayerEntity player = client.player;
		if (player == null || client.world == null || client.interactionManager == null) {
			return;
		}

		if (cooldown > 0) {
			cooldown--;
			return;
		}

		if (stepIndex >= activeBlueprint.steps.size()) {
			player.sendMessage(new TranslatableText("hitboxdebug.message.builder_done"), false);
			activeBlueprint = null;
			return;
		}

		FarmBlueprint.Step step = activeBlueprint.steps.get(stepIndex);
		BlockPos targetPos = origin.add(step.relativePos());

		if (player.squaredDistanceTo(Vec3d.ofCenter(targetPos)) > REACH * REACH) {
			// The user has to physically walk into range — we never teleport
			// or place at a distance; just wait here until they do.
			waitingForMove = true;
			return;
		}
		waitingForMove = false;

		Hand hand = findItemHand(player, step.item());
		if (hand == null) {
			player.sendMessage(new TranslatableText("hitboxdebug.message.builder_missing_items",
					step.item().getName().getString()), false);
			return;
		}

		Direction side = pickPlacementSide(client, targetPos);
		if (side == null) {
			return;
		}

		BlockHitResult hitResult = new BlockHitResult(Vec3d.ofCenter(targetPos), side, targetPos, false);
		client.interactionManager.interactBlock(player, client.world, hand, hitResult);
		player.swingHand(hand);

		stepIndex++;
		cooldown = TICKS_BETWEEN_PLACEMENTS;
	}

	public boolean isWaitingForPlayerToMove() {
		return waitingForMove;
	}

	private Hand findItemHand(ClientPlayerEntity player, Item item) {
		for (Hand hand : Hand.values()) {
			ItemStack stack = player.getStackInHand(hand);
			if (stack.isOf(item)) {
				return hand;
			}
		}
		for (int i = 0; i < player.getInventory().main.size(); i++) {
			if (player.getInventory().main.get(i).isOf(item)) {
				player.getInventory().selectedSlot = Math.min(i, 8);
				return Hand.MAIN_HAND;
			}
		}
		return null;
	}

	/** Picks a neighboring, already-solid block to "click" against, matching
	 *  how a player places a block onto an existing surface. */
	private Direction pickPlacementSide(MinecraftClient client, BlockPos targetPos) {
		for (Direction dir : Direction.values()) {
			BlockPos neighbor = targetPos.offset(dir);
			if (!client.world.getBlockState(neighbor).isAir()) {
				return dir.getOpposite();
			}
		}
		return null;
	}
}
