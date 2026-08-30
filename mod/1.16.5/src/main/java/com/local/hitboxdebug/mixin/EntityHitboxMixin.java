package com.local.hitboxdebug.mixin;

import com.local.hitboxdebug.HitboxDebugClient;
import com.local.hitboxdebug.util.SafetyGuard;
import net.minecraft.client.MinecraftClient;
import net.minecraft.entity.Entity;
import net.minecraft.entity.mob.HostileEntity;
import net.minecraft.entity.passive.AnimalEntity;
import net.minecraft.entity.player.PlayerEntity;
import net.minecraft.util.math.Box;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/**
 * Expands the hit-detection box of nearby animals/hostile mobs — never
 * {@link PlayerEntity} — when the "hitbox enlargement" toggle is on. Runs
 * client-side only, which in singleplayer is the same JVM as the
 * integrated server, so it genuinely changes local hit detection; on any
 * real multiplayer connection the remote server's own entity boxes are
 * authoritative and are not touched by this. {@link SafetyGuard} still
 * turns it off the moment another real player is nearby (LAN/co-op),
 * consistent with every other automation feature in this mod.
 */
@Mixin(Entity.class)
public abstract class EntityHitboxMixin {

	@Inject(method = "getBoundingBox()Lnet/minecraft/util/math/Box;", at = @At("RETURN"), cancellable = true)
	private void hitboxdebug$expandMobBoundingBox(CallbackInfoReturnable<Box> cir) {
		Object self = this;
		if (!(self instanceof AnimalEntity) && !(self instanceof HostileEntity)) {
			return;
		}
		if (self instanceof PlayerEntity) {
			return; // never reached given the check above, kept as an explicit guarantee
		}

		if (!HitboxDebugClient.CONFIG.hitboxExpandEnabled) {
			return;
		}

		MinecraftClient client = MinecraftClient.getInstance();
		if (client == null || !SafetyGuard.canAutomate(client)) {
			return;
		}

		double amount = HitboxDebugClient.CONFIG.hitboxExpandAmount;
		cir.setReturnValue(cir.getReturnValue().expand(amount));
	}
}
