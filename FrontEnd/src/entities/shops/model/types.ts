export type Shop = {
  id: number;
  name: string;
  address: string;
  description?: string | null;
};

export type SaveShopInput = {
  name: string;
  address: string;
  description?: string;
};

export type ShopsListParams = {
  search?: string;
};
