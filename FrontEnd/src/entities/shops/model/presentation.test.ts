import { describe, expect, test } from "vitest";
import {
  getShopDetailsState,
  getShopDisplayName,
  getShopInitials,
  getShopProfileDetails,
} from "./presentation";
import type { Shop } from "./types";

const shop: Shop = {
  id: 1,
  name: "Central Market",
  address: "Main Street 1",
  description: "Flagship location",
};

describe("shop presentation model", () => {
  test("formats display name and initials with fallbacks", () => {
    expect(getShopDisplayName(shop)).toBe("Central Market");
    expect(getShopDisplayName({ name: "  " }, "Unknown shop")).toBe("Unknown shop");

    expect(getShopInitials(shop)).toBe("CM");
    expect(getShopInitials({ name: "North" })).toBe("N");
    expect(getShopInitials({ name: "  " }, "??")).toBe("??");
  });

  test("describes available profile details for every address and description combination", () => {
    expect(getShopDetailsState(shop)).toBe("Address and description available");
    expect(getShopDetailsState({ address: "Main Street", description: " " })).toBe("Address available");
    expect(getShopDetailsState({ address: " ", description: "Seasonal shop" })).toBe("Description available");
    expect(getShopDetailsState({ address: " ", description: " " })).toBe("Shop details missing");
    expect(getShopDetailsState(null)).toBe("Shop details missing");
  });

  test("returns trimmed profile details and nulls for missing values", () => {
    expect(getShopProfileDetails({ address: "  Main Street 1  ", description: "  Flagship  " })).toEqual([
      { key: "address", label: "Address", value: "Main Street 1" },
      { key: "description", label: "Description", value: "Flagship" },
    ]);

    expect(getShopProfileDetails({ address: " ", description: null })).toEqual([
      { key: "address", label: "Address", value: null },
      { key: "description", label: "Description", value: null },
    ]);
  });
});
