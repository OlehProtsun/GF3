import { t } from "@shared/i18n";
import { useMemo } from "react";
import type { ChangeEvent } from "react";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
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
    nextErrors.name = t("Name is required");
  }

  if (!form.address.trim()) {
    nextErrors.address = t("Address is required");
  }

  return nextErrors;
}

export function useShopForm(shop?: ShopFormSource | null, isCreate = false) {
  const sourceKey = useMemo(
    () =>
      shop
        ? `shop:${shop.name}:${shop.address}:${shop.description ?? ""}`
        : isCreate
          ? "create"
          : "empty",
    [isCreate, shop],
  );
  const initialDraft = useMemo(
    () => ({
      form: createShopFormState(shop),
      errors: {} as ShopFormErrors,
    }),
    [shop],
  );
  const { value: draft, setValue: setDraft } = useSyncedDraft(sourceKey, initialDraft);
  const { form, errors } = draft;

  const handleFieldChange =
    (field: keyof ShopFormState) =>
    (event: ChangeEvent<ShopFormFieldElement>): void => {
      setDraft((current) => ({
        form: { ...current.form, [field]: event.target.value },
        errors: current.errors,
      }));
    };

  const validate = () => {
    const nextErrors = validateShopForm(form);

    setDraft((current) => ({
      ...current,
      errors: nextErrors,
    }));

    return Object.keys(nextErrors).length === 0;
  };

  return {
    form,
    errors,
    handleFieldChange,
    validate,
  };
}
