package com.local.hitboxdebug.feature.farmbuilder;

import net.minecraft.item.Items;
import net.minecraft.util.math.BlockPos;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Well-known, widely published singleplayer farm layouts (the kind found
 * in any Minecraft farming tutorial/wiki article) expressed as block-by-
 * block placement lists for {@link AutoFarmBuilder}:
 *
 * <ul>
 *   <li>{@link #simpleCropField()} — classic 9x9 tillable plot hydrated by
 *       one central water source (water reaches farmland up to 4 blocks
 *       away), the standard wheat/carrot/potato layout.</li>
 *   <li>{@link #sugarCaneFarm()} — a row of dirt/sand next to a water
 *       channel, one sugarcane planted per column, the standard
 *       no-piston/no-observer AFK-able design.</li>
 *   <li>{@link #animalPen()} — a fenced 7x7 enclosure with a gate, for
 *       breeding/holding animals.</li>
 * </ul>
 */
public final class Blueprints {

	public enum Type {
		SIMPLE_CROP_FIELD,
		SUGAR_CANE_FARM,
		ANIMAL_PEN
	}

	private Blueprints() {
	}

	public static FarmBlueprint get(Type type) {
		switch (type) {
			case SIMPLE_CROP_FIELD:
				return simpleCropField();
			case SUGAR_CANE_FARM:
				return sugarCaneFarm();
			case ANIMAL_PEN:
				return animalPen();
			default:
				throw new IllegalArgumentException("Unknown blueprint type: " + type);
		}
	}

	/** 9x9 area, y=0 is the farmland/water layer. Water source at the center (4,0,4). */
	private static FarmBlueprint simpleCropField() {
		List<FarmBlueprint.Step> steps = new ArrayList<>();
		for (int x = 0; x < 9; x++) {
			for (int z = 0; z < 9; z++) {
				boolean center = x == 4 && z == 4;
				steps.add(new FarmBlueprint.Step(new BlockPos(x, 0, z),
						center ? Items.WATER_BUCKET : Items.FARMLAND));
			}
		}
		for (int x = 0; x < 9; x++) {
			for (int z = 0; z < 9; z++) {
				if (x == 4 && z == 4) {
					continue;
				}
				steps.add(new FarmBlueprint.Step(new BlockPos(x, 1, z), Items.WHEAT_SEEDS));
			}
		}
		return new FarmBlueprint("Simple Crop Field (9x9)", steps);
	}

	/** A straight 8-long channel: water on one side, sand + sugarcane on the other. */
	private static FarmBlueprint sugarCaneFarm() {
		List<FarmBlueprint.Step> steps = new ArrayList<>();
		int length = 8;
		for (int x = 0; x < length; x++) {
			steps.add(new FarmBlueprint.Step(new BlockPos(x, 0, 0), Items.WATER_BUCKET));
			steps.add(new FarmBlueprint.Step(new BlockPos(x, 0, 1), Items.SAND));
			steps.add(new FarmBlueprint.Step(new BlockPos(x, 1, 1), Items.SUGAR_CANE));
		}
		return new FarmBlueprint("Sugar Cane Row (8 blocks)", steps);
	}

	/** 7x7 fenced pen with a single gate on the south side. */
	private static FarmBlueprint animalPen() {
		List<FarmBlueprint.Step> steps = new ArrayList<>();
		int size = 7;
		for (int x = 0; x < size; x++) {
			for (int z = 0; z < size; z++) {
				boolean edge = x == 0 || z == 0 || x == size - 1 || z == size - 1;
				boolean gate = x == size / 2 && z == size - 1;
				if (gate) {
					steps.add(new FarmBlueprint.Step(new BlockPos(x, 1, z), Items.OAK_FENCE_GATE));
				} else if (edge) {
					steps.add(new FarmBlueprint.Step(new BlockPos(x, 1, z), Items.OAK_FENCE));
				}
			}
		}
		return new FarmBlueprint("Animal Pen (7x7)", steps);
	}

	public static Map<Type, String> displayNames() {
		return Map.of(
				Type.SIMPLE_CROP_FIELD, "Simple Crop Field",
				Type.SUGAR_CANE_FARM, "Sugar Cane Row",
				Type.ANIMAL_PEN, "Animal Pen"
		);
	}
}
