export type ShopDto = {
  id: number;
  name: string;
  address: string;
  description?: string | null;
};

export type SaveShopDto = {
  name: string;
  address: string;
  description?: string;
};
