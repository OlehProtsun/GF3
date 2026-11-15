import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { shopsApi } from "./shopsApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("shops api", () => {
  test("passes read signal through list and byId requests", () => {
    const controller = new AbortController();

    shopsApi.list(controller.signal);
    shopsApi.byId(4, controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "shops", { signal: controller.signal });
    expect(requestMock).toHaveBeenNthCalledWith(2, "shops/4", { signal: controller.signal });
  });

  test("trims create and update payloads while dropping blank optional descriptions", () => {
    shopsApi.create({
      name: "  Central Shop  ",
      address: "  Main Street 1  ",
      description: "   ",
    });
    shopsApi.update(4, {
      name: " North Shop ",
      address: " North Street 2 ",
      description: "  Seasonal location  ",
    });

    expect(requestMock).toHaveBeenNthCalledWith(1, "shops", {
      method: "POST",
      body: {
        name: "Central Shop",
        address: "Main Street 1",
        description: undefined,
      },
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "shops/4", {
      method: "PUT",
      body: {
        name: "North Shop",
        address: "North Street 2",
        description: "Seasonal location",
      },
    });
  });

  test("uses the delete endpoint without a request body", () => {
    shopsApi.remove(4);

    expect(requestMock).toHaveBeenCalledWith("shops/4", { method: "DELETE" });
  });
});
