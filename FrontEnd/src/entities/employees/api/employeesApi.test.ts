import { beforeEach, describe, expect, test, vi } from "vitest";

const requestMock = vi.hoisted(() => vi.fn());

vi.mock("@shared/api/httpClient", () => ({
  request: requestMock,
}));

import { employeesApi } from "./employeesApi";

beforeEach(() => {
  requestMock.mockReset();
});

describe("employees api", () => {
  test("passes read signal through list and byId requests", () => {
    const controller = new AbortController();

    employeesApi.list(controller.signal);
    employeesApi.byId(7, controller.signal);

    expect(requestMock).toHaveBeenNthCalledWith(1, "employees", { signal: controller.signal });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employees/7", { signal: controller.signal });
  });

  test("trims create and update payloads while dropping blank optional account fields", () => {
    employeesApi.create({
      firstName: "  Ada  ",
      lastName: "  Lovelace  ",
      phone: "  +48123456789  ",
      email: "   ",
      username: "  ada  ",
      password: "",
    });
    employeesApi.update(7, {
      firstName: " Grace ",
      lastName: " Hopper ",
      phone: " ",
      email: " grace@example.com ",
      username: " ",
      password: "654321",
    });

    expect(requestMock).toHaveBeenNthCalledWith(1, "employees", {
      method: "POST",
      body: {
        firstName: "Ada",
        lastName: "Lovelace",
        phone: "+48123456789",
        email: undefined,
        username: "ada",
        password: undefined,
      },
    });
    expect(requestMock).toHaveBeenNthCalledWith(2, "employees/7", {
      method: "PUT",
      body: {
        firstName: "Grace",
        lastName: "Hopper",
        phone: undefined,
        email: "grace@example.com",
        username: undefined,
        password: "654321",
      },
    });
  });

  test("uses the delete endpoint without a request body", () => {
    employeesApi.remove(7);

    expect(requestMock).toHaveBeenCalledWith("employees/7", { method: "DELETE" });
  });
});
