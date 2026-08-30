package com.local.hitboxdebug.feature.farmbuilder;

import net.minecraft.item.Item;
import net.minecraft.util.math.BlockPos;

import java.util.List;

/**
 * A single farm design: an ordered list of (position, item-to-place) steps
 * relative to an origin corner. Order matters — e.g. water source blocks
 * are placed before the farmland/crops that depend on them, matching how
 * a player would build the design by hand.
 */
public final class FarmBlueprint {

	public final String name;
	public final List<Step> steps;

	public FarmBlueprint(String name, List<Step> steps) {
		this.name = name;
		this.steps = steps;
	}

	public static final class Step {
		private final BlockPos relativePos;
		private final Item item;

		public Step(BlockPos relativePos, Item item) {
			this.relativePos = relativePos;
			this.item = item;
		}

		public BlockPos relativePos() {
			return relativePos;
		}

		public Item item() {
			return item;
		}
	}
}
