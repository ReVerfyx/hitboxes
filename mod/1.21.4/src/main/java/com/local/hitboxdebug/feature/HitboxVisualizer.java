package com.local.hitboxdebug.feature;

import com.local.hitboxdebug.HitboxDebugClient;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderContext;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.render.RenderLayer;
import net.minecraft.client.render.VertexConsumer;
import net.minecraft.client.render.VertexConsumerProvider;
import net.minecraft.client.render.VertexRendering;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.entity.Entity;
import net.minecraft.entity.player.PlayerEntity;
import net.minecraft.util.math.Box;
import net.minecraft.util.math.Vec3d;

/**
 * Pure visual overlay: draws the real, unmodified bounding box of nearby
 * mobs (never players) so it can be used to line up hits/farms. It does
 * not read or change any entity's actual hitbox size.
 */
public final class HitboxVisualizer {

	private static final double RENDER_RADIUS = 24.0;

	public void render(WorldRenderContext context) {
		if (!HitboxDebugClient.CONFIG.hitboxVisualizerEnabled) {
			return;
		}

		MinecraftClient client = MinecraftClient.getInstance();
		if (client.player == null || client.world == null) {
			return;
		}

		Vec3d camera = context.camera().getPos();
		MatrixStack matrices = context.matrixStack();
		VertexConsumerProvider consumers = context.consumers();
		if (matrices == null || consumers == null) {
			return;
		}

		// The modern shader-based pipeline draws via a shared
		// VertexConsumerProvider the engine already has open for this
		// frame's translucent pass — submit into it and let the engine
		// flush it, instead of hand-managing a Tessellator/BufferBuilder
		// and GL state the way the 1.16.5 build (which predates this
		// pipeline) has to.
		VertexConsumer buffer = consumers.getBuffer(RenderLayer.getLines());

		Box searchBox = client.player.getBoundingBox().expand(RENDER_RADIUS);
		for (Entity entity : client.world.getEntitiesByClass(Entity.class, searchBox,
				e -> !(e instanceof PlayerEntity) && e.isAlive())) {
			Box box = entity.getBoundingBox().offset(-camera.x, -camera.y, -camera.z);
			VertexRendering.drawBox(matrices, buffer, box, 1.0f, 0.4f, 0.1f, 0.8f);
		}
	}
}
