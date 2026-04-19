import { useMemo, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  useCreateShopMutation,
  useShopByIdQuery,
  useUpdateShopMutation,
} from "@entities/shops/api/queries";
import { createShopFormState, useShopForm } from "@entities/shops/model/form";
import { ShopDetailsForm } from "@entities/shops/ui/ShopDetailsForm";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { PageHeader } from "@shared/ui/PageHeader";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import styles from "./ShopEditPage.module.css";

export function ShopEditPage() {
  const navigate = useNavigate();
  const { shopId } = useParams<{ shopId: string }>();
  const isCreate = !shopId;
  const id = shopId ? Number(shopId) : null;

  const shopQuery = useShopByIdQuery(!isCreate && Number.isFinite(id) ? id : null);
  const createMutation = useCreateShopMutation();
  const updateMutation = useUpdateShopMutation();
  const { form, errors, handleFieldChange, validate } = useShopForm(shopQuery.data, isCreate);

  const backTo = useMemo(() => {
    if (isCreate) {
      return "/shop";
    }

    return `/shop/${id}`;
  }, [id, isCreate]);

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const hasLoadError = !isCreate && Boolean(shopQuery.error) && !shopQuery.isLoading && !shopQuery.data;
  const initialFormSnapshot = useMemo(
    () => stableSerialize(createShopFormState(shopQuery.data)),
    [shopQuery.data],
  );
  const currentFormSnapshot = useMemo(() => stableSerialize(form), [form]);
  const hasUnsavedChanges = useMemo(() => {
    if (isCreate) {
      return currentFormSnapshot !== initialFormSnapshot;
    }

    return Boolean(shopQuery.data) && currentFormSnapshot !== initialFormSnapshot;
  }, [currentFormSnapshot, initialFormSnapshot, isCreate, shopQuery.data]);
  const { dialog: unsavedChangesDialog, runWithoutPrompt } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !isSaving,
  });

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!validate()) return;

    if (isCreate) {
      createMutation.mutate(form, {
        onSuccess: (created) => runWithoutPrompt(() => navigate(`/shop/${created.id}`)),
      });
      return;
    }

    if (!id) return;
    updateMutation.mutate({ id, payload: form }, { onSuccess: () => runWithoutPrompt(() => navigate(`/shop/${id}`)) });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={isCreate ? "Add Shop" : "Edit Shop"}
        subtitle={isCreate ? "Create new shop record" : "Update shop information"}
        backTo={backTo}
      />

      <ShopDetailsForm
        form={form}
        errors={errors}
        isLoading={!isCreate && shopQuery.isLoading}
        hasLoadError={hasLoadError}
        isSaving={isSaving}
        onFieldChange={handleFieldChange}
        onCancel={() => navigate(backTo)}
        onSubmit={onSubmit}
      />

      <SavingOverlay active={isSaving} />
      {unsavedChangesDialog}
    </div>
  );
}
