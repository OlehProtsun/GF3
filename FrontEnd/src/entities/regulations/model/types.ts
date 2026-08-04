export type RegulationDocument = {
  id: number;
  title: string;
  version: string;
  message: string;
  pdfFileName: string;
  pdfSha256: string;
  isPublished: boolean;
  publishedAtUtc?: string | null;
  createdByManagerName: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  acceptanceCount: number;
};

export type RegulationAcceptance = {
  id: number;
  regulationDocumentId: number;
  regulationTitle: string;
  regulationVersion: string;
  pdfSha256: string;
  accountRole: "manager" | "employee";
  accountId: number;
  username: string;
  displayName: string;
  acceptedAtUtc: string;
};

export type SaveRegulationInput = {
  title: string;
  version: string;
  message: string;
  pdf?: File | null;
};
