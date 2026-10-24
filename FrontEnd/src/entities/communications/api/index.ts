export type {
  CommunicationMessageDto,
  CreateCommunicationMessageDto,
  UpdateCommunicationMessageDto,
} from "./dto";
export { communicationsApi } from "./communicationsApi";
export {
  useCreateCommunicationMutation,
  useDeleteCommunicationMutation,
  useDismissEmployeeCommunicationMutation,
  useEmployeePendingCommunicationsQuery,
  useManagerCommunicationsQuery,
  useUpdateCommunicationMutation,
} from "./queries";
