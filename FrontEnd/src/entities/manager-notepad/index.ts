export { managerNotepadApi } from "./api/managerNotepadApi";
export {
  useCreateManagerNoteMutation,
  useDeleteManagerNoteMutation,
  useManagerNotepadQuery,
  useSaveManagerNotepadStateMutation,
  useManagerNoteShareTargetsQuery,
  useManagerNoteVersionsQuery,
  useUpdateManagerNoteMetadataMutation,
  useRestoreManagerNoteVersionMutation,
  useUpdateManagerNoteMutation,
} from "./api/queries";
export type {
  ManagerNote,
  ManagerNoteColor,
  ManagerNoteVisibility,
  ManagerNoteShareTarget,
  ManagerNoteVersion,
  UpdateManagerNoteMetadataInput,
  ManagerNotepad,
  ManagerNotepadState,
  SaveManagerNoteInput,
} from "./model/types";
