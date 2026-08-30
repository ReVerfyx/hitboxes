package com.local.hitboxdebug.util;

import com.local.hitboxdebug.HitboxDebugClient;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.client.world.ClientWorld;
import net.minecraft.entity.player.PlayerEntity;

/**
 * Central "is it safe to automate right now" check shared by every
 * automation feature (mob auto-farm, farm builder). Any real player other
 * than the local client counts — including LAN/co-op sessions — which is
 * the point: these tools are for grinding against mobs/blocks, not for
 * acting while another person is around.
 */
public final class SafetyGuard {

	private SafetyGuard() {
	}

	public static boolean otherPlayersNearby(MinecraftClient client) {
		ClientPlayerEntity self = client.player;
		ClientWorld world = client.world;
		if (self == null || world == null) {
			return false;
		}

		double radius = HitboxDebugClient.CONFIG.otherPlayerSafetyRadius;
		for (PlayerEntity player : world.getPlayers()) {
			if (player == self) {
				continue;
			}
			if (player.squaredDistanceTo(self) <= radius * radius) {
				return true;
			}
		}
		return false;
	}

	/** Convenience: true when automation is currently allowed to act. */
	public static boolean canAutomate(MinecraftClient client) {
		return !otherPlayersNearby(client);
	}
}
