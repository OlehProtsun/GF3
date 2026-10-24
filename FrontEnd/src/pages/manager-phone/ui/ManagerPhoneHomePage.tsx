import { HomePage } from "@pages/home";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneHomePage() {
  return <ManagerPhonePage><HomePage phoneMode /></ManagerPhonePage>;
}
