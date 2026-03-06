import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useDeleteShopMutation, useShopByIdQuery } from "@entities/shops/api/queries";
import { ShopProfileCard } from "@entities/shops/ui/ShopProfileCard";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ShopProfilePage.module.css";

export function ShopProfilePage() {
  const navigate = useNavigate();
  const { shopId } = useParams<{ shopId: string }>();
  const id = shopId ? Number(shopId) : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  const shopQuery = useShopByIdQuery(Number.isFinite(id) ? id : null);
  const deleteMutation = useDeleteShopMutation();

  const shop = shopQuery.data;
  const hasValidId = Number.isFinite(id);
  const isShopLoading = hasValidId && !shop && (shopQuery.isLoading || shopQuery.isFetching);
  const hasLoadError = !shop && (!hasValidId || (!isShopLoading && (shopQuery.isError || Boolean(shopQuery.error))));

  const handleDeleteConfirm = () => {
    if (!id) return;

    deleteMutation.mutate(id, {
      onSuccess: () => navigate("/shop"),
    });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title="Shop Profile"
        subtitle="View shop details, location information and record status"
        backTo="/shop"
      />

      <ShopProfileCard
        shop={shop}
        isLoading={isShopLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteMutation.isPending}
        onEditShop={shopIdValue => navigate(`/shop/${shopIdValue}/edit`)}
        onDeleteShop={() => setIsDeleteOpen(true)}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete shop"
        message="Are you sure you want to delete this shop? This action cannot be undone."
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
      />
    </div>
  );
}
