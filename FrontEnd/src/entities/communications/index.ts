export type {
  CommunicationMessageDto,
  CreateCommunicationMessageDto,
  UpdateCommunicationMessageDto,
} from "./api";
export { maxCommunicationGifBytes } from "./api/communicationsApi";
export { CommunicationGif, CommunicationPhoto } from "./ui/CommunicationGif";
export { CommunicationPreview } from "./ui/CommunicationPreview";
export {
  communicationsApi,
  useCreateCommunicationMutation,
  useDeleteCommunicationMutation,
  useDismissEmployeeCommunicationMutation,
  useEmployeePendingCommunicationsQuery,
  useManagerCommunicationsQuery,
  useUpdateCommunicationMutation,
} from "./api";

export { maxCommunicationBlocks, maxCommunicationUploadBytes, type CommunicationDraftBlock } from "./api/dto";
export { getCommunicationBlocks } from "./model/contentBlocks";
