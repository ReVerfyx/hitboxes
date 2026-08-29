package com.local.hitboxdebug.feature;

import com.local.hitboxdebug.HitboxDebugClient;
import com.local.hitboxdebug.util.SafetyGuard;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.entity.Entity;
import net.minecraft.entity.mob.HostileEntity;
import net.minecraft.entity.passive.AnimalEntity;
import net.minecraft.entity.player.PlayerEntity;
import net.minecraft.network.packet.c2s.play.HandSwingC2SPacket;
import net.minecraft.util.Hand;
import net.minecraft.util.hit.EntityHitResult;
import net.minecraft.util.hit.HitResult;
import net.minecraft.util.math.Box;
import net.minecraft.util.math.Vec3d;

import java.util.Comparator;
import java.util.List;
import java.util.Optional;

/**
 * Auto-attacks nearby animals/hostile mobs only — never {@link PlayerEntity}.
 * Disarms itself whenever {@link SafetyGuard} sees another real player
 * within range, so it behaves like an idle grinding macro and never like a
 * combat cheat against a person.
 */
public final class AutoFarmMobs {

	private static final int ATTACK_COOLDOWN_TICKS = 10; // matches a plain hand's real attack speed
	private int cooldown = 0;

	public void tick(MinecraftClient client) {
		if (!HitboxDebugClient.CONFIG.autoFarmMobsEnabled) {
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

		double radius = HitboxDebugClient.CONFIG.autoFarmRadius;
		Box searchBox = player.getBoundingBox().expand(radius);

		Optional<Entity> target = client.world.getEntitiesByClass(Entity.class, searchBox,
				e -> (e instanceof AnimalEntity || e instanceof HostileEntity)
						&& !(e instanceof PlayerEntity)
						&& e.isAlive()
						&& player.squaredDistanceTo(e) <= radius * radius)
				.stream()
				.min(Comparator.comparingDouble(player::squaredDistanceTo));

		if (target.isEmpty()) {
			return;
		}

		Entity mob = target.get();
		if (!hasLineOfSight(client, mob)) {
			return;
		}

		client.interactionManager.attackEntity(player, mob);
		player.swingHand(Hand.MAIN_HAND);
		client.getNetworkHandler().sendPacket(new HandSwingC2SPacket(Hand.MAIN_HAND));

		cooldown = ATTACK_COOLDOWN_TICKS;
	}

	private boolean hasLineOfSight(MinecraftClient client, Entity mob) {
		ClientPlayerEntity player = client.player;
		if (player == null) {
			return false;
		}
		double reach = 3.0; // survival attack reach, deliberately not extended
		if (player.squaredDistanceTo(mob) > reach * reach) {
			return false;
		}
		Vec3d eyePos = player.getCameraPosVec(1.0f);
		Vec3d targetPos = mob.getBoundingBox().getCenter();
		HitResult hit = client.world.raycast(new net.minecraft.world.RaycastContext(
				eyePos, targetPos,
				net.minecraft.world.RaycastContext.ShapeType.COLLIDER,
				net.minecraft.world.RaycastContext.FluidHandling.NONE,
				player));
		return hit.getType() == HitResult.Type.NONE
				|| (hit instanceof EntityHitResult eh && eh.getEntity() == mob);
	}
}
