export type AvailabilityBind = {
  id: number;
  key: string;
  value: string;
  isActive: boolean;
};

export type SaveAvailabilityBindInput = {
  key: string;
  value: string;
  isActive: boolean;
};
