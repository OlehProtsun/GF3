export type {
  CommunicationMessageDto,
  CreateCommunicationMessageDto,
  UpdateCommunicationMessageDto,
} from "./api";
export {
  communicationsApi,
  useCreateCommunicationMutation,
  useDeleteCommunicationMutation,
  useDismissEmployeeCommunicationMutation,
  useEmployeePendingCommunicationsQuery,
  useManagerCommunicationsQuery,
  useUpdateCommunicationMutation,
} from "./api";
