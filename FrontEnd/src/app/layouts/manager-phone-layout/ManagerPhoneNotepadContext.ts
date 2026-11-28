import { createContext } from "react";

export const ManagerPhoneNotepadContext = createContext<((launcher?: HTMLElement) => void) | null>(null);
