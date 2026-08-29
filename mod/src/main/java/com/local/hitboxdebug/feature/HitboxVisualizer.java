package com.local.hitboxdebug.feature;

import com.local.hitboxdebug.HitboxDebugClient;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderContext;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.render.BufferBuilder;
import net.minecraft.client.render.Tessellator;
import net.minecraft.client.render.VertexFormat;
import net.minecraft.client.render.VertexFormats;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.entity.Entity;
import net.minecraft.entity.player.PlayerEntity;
import net.minecraft.util.math.Box;
import net.minecraft.util.math.Matrix4f;
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

		MinecraftClient client = context.gameRenderer().getClient();
		if (client.player == null || client.world == null) {
			return;
		}

		Vec3d camera = context.camera().getPos();
		MatrixStack matrices = context.matrixStack();
		if (matrices == null) {
			return;
		}

		net.minecraft.client.render.RenderLayer.getLines();
		BufferBuilder buffer = Tessellator.getInstance().getBuffer();

		com.mojang.blaze3d.systems.RenderSystem.setShader(net.minecraft.client.render.GameRenderer::getPositionColorShader);
		com.mojang.blaze3d.systems.RenderSystem.lineWidth(2.0f);
		com.mojang.blaze3d.systems.RenderSystem.disableTexture();
		com.mojang.blaze3d.systems.RenderSystem.enableBlend();
		com.mojang.blaze3d.systems.RenderSystem.defaultBlendFunc();
		buffer.begin(VertexFormat.DrawMode.LINES, VertexFormats.POSITION_COLOR);

		Box searchBox = client.player.getBoundingBox().expand(RENDER_RADIUS);
		for (Entity entity : client.world.getEntitiesByClass(Entity.class, searchBox,
				e -> !(e instanceof PlayerEntity) && e.isAlive())) {
			Box box = entity.getBoundingBox().offset(-camera.x, -camera.y, -camera.z);
			drawBox(buffer, matrices, box, 1.0f, 0.4f, 0.1f, 0.8f);
		}

		Tessellator.getInstance().draw();
		com.mojang.blaze3d.systems.RenderSystem.enableTexture();
		com.mojang.blaze3d.systems.RenderSystem.disableBlend();
	}

	private void drawBox(BufferBuilder buffer, MatrixStack matrices, Box b,
			float r, float g, float bCol, float a) {
		Matrix4f m = matrices.peek().getPositionMatrix();
		float[][] corners = {
				{(float) b.minX, (float) b.minY, (float) b.minZ}, {(float) b.maxX, (float) b.minY, (float) b.minZ},
				{(float) b.maxX, (float) b.minY, (float) b.minZ}, {(float) b.maxX, (float) b.minY, (float) b.maxZ},
				{(float) b.maxX, (float) b.minY, (float) b.maxZ}, {(float) b.minX, (float) b.minY, (float) b.maxZ},
				{(float) b.minX, (float) b.minY, (float) b.maxZ}, {(float) b.minX, (float) b.minY, (float) b.minZ},
				{(float) b.minX, (float) b.maxY, (float) b.minZ}, {(float) b.maxX, (float) b.maxY, (float) b.minZ},
				{(float) b.maxX, (float) b.maxY, (float) b.minZ}, {(float) b.maxX, (float) b.maxY, (float) b.maxZ},
				{(float) b.maxX, (float) b.maxY, (float) b.maxZ}, {(float) b.minX, (float) b.maxY, (float) b.maxZ},
				{(float) b.minX, (float) b.maxY, (float) b.maxZ}, {(float) b.minX, (float) b.maxY, (float) b.minZ},
				{(float) b.minX, (float) b.minY, (float) b.minZ}, {(float) b.minX, (float) b.maxY, (float) b.minZ},
				{(float) b.maxX, (float) b.minY, (float) b.minZ}, {(float) b.maxX, (float) b.maxY, (float) b.minZ},
				{(float) b.maxX, (float) b.minY, (float) b.maxZ}, {(float) b.maxX, (float) b.maxY, (float) b.maxZ},
				{(float) b.minX, (float) b.minY, (float) b.maxZ}, {(float) b.minX, (float) b.maxY, (float) b.maxZ},
		};
		for (float[] c : corners) {
			buffer.vertex(m, c[0], c[1], c[2]).color(r, g, bCol, a).next();
		}
	}
}
