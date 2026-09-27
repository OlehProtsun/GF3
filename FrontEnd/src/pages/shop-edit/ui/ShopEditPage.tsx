import { t } from "@shared/i18n";
import { useMemo, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  buildManagerEditLockMessage,
  managerEditResourceTypes,
  useManagerEditLocks,
} from "@app/providers/PresenceProvider";
import {
  useCreateShopMutation,
  useShopByIdQuery,
  useUpdateShopMutation,
} from "@entities/shops/api/queries";
import { createShopFormState, useShopForm } from "@entities/shops/model/form";
import { ShopDetailsForm } from "@entities/shops/ui/ShopDetailsForm";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ManagerEditLockDialog } from "@shared/ui/ManagerEditLockDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import styles from "./ShopEditPage.module.css";

export function ShopEditPage() {
  const navigate = useNavigate();
  const { shopId } = useParams<{ shopId: string }>();
  const isCreate = !shopId;
  const id = shopId ? Number(shopId) : null;
  const editLockTargets = useMemo(
    () => !isCreate && Number.isFinite(id)
      ? [{
        resourceType: managerEditResourceTypes.shop,
        resourceId: String(id),
      }]
      : [],
    [id, isCreate],
  );
  const { lockedByOtherState, isCheckingLocks } = useManagerEditLocks(editLockTargets);
  const editLockMessage = lockedByOtherState
    ? buildManagerEditLockMessage(lockedByOtherState, t("This shop"))
    : null;
  const canEdit = !editLockMessage && !isCheckingLocks;

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

  const handleEditLockDialogClose = () => {
    runWithoutPrompt(() => navigate(backTo));
  };

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (editLockMessage) return;
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
        title={isCreate ? t("Add Shop") : t("Edit Shop")}
        subtitle={isCreate ? t("Create new shop record") : t("Update shop information")}
        backTo={backTo}
      />

      {isCheckingLocks ? (
        <ManagerEditLockDialog
          open
          title={t("Checking edit access")}
          message={t("Please wait while we check whether this shop can be edited.")}
        />
      ) : null}

      {editLockMessage ? (
        <ManagerEditLockDialog
          open
          message={editLockMessage}
          actionText={t("Back to shop")}
          onClose={handleEditLockDialogClose}
        />
      ) : null}

      {canEdit ? (
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
      ) : null}

      <SavingOverlay active={isSaving} />
      {unsavedChangesDialog}
    </div>
  );
}
