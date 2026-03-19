import { useEffect, useState } from "react";
import type { ChangeEvent } from "react";
import type { Shop } from "./types";

export type ShopFormFieldElement = HTMLInputElement | HTMLTextAreaElement;

export type ShopFormState = {
  name: string;
  address: string;
  description: string;
};

export type ShopFormErrors = Partial<Record<keyof ShopFormState, string>>;

type ShopFormSource = Pick<Shop, "name" | "address" | "description">;

const EMPTY_FORM: ShopFormState = {
  name: "",
  address: "",
  description: "",
};

export function createShopFormState(shop?: ShopFormSource | null): ShopFormState {
  if (!shop) {
    return EMPTY_FORM;
  }

  return {
    name: shop.name,
    address: shop.address,
    description: shop.description ?? "",
  };
}

export function validateShopForm(form: ShopFormState): ShopFormErrors {
  const nextErrors: ShopFormErrors = {};

  if (!form.name.trim()) {
    nextErrors.name = "Name is required";
  }

  if (!form.address.trim()) {
    nextErrors.address = "Address is required";
  }

  return nextErrors;
}

export function useShopForm(shop?: ShopFormSource | null, isCreate = false) {
  const [form, setForm] = useState<ShopFormState>(EMPTY_FORM);
  const [errors, setErrors] = useState<ShopFormErrors>({});

  useEffect(() => {
    if (shop) {
      setForm(createShopFormState(shop));
      setErrors({});
      return;
    }

    if (isCreate) {
      setForm(EMPTY_FORM);
      setErrors({});
    }
  }, [shop, isCreate]);

  const handleFieldChange =
    (field: keyof ShopFormState) =>
    (event: ChangeEvent<ShopFormFieldElement>): void => {
      setForm((prev) => ({ ...prev, [field]: event.target.value }));
    };

  const validate = () => {
    const nextErrors = validateShopForm(form);
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  return {
    form,
    errors,
    handleFieldChange,
    validate,
  };
}
