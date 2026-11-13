export { managerNotepadApi } from "./api/managerNotepadApi";
export {
  useCreateManagerNoteMutation,
  useDeleteManagerNoteMutation,
  useManagerNotepadQuery,
  useSaveManagerNotepadStateMutation,
  useUpdateManagerNoteMutation,
} from "./api/queries";
export type {
  ManagerNote,
  ManagerNoteColor,
  ManagerNotepad,
  ManagerNotepadState,
  SaveManagerNoteInput,
} from "./model/types";
