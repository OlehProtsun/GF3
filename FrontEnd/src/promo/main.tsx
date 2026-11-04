import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { PromoApp } from "./PromoApp";

createRoot(document.getElementById("promo-root")!).render(
  <StrictMode><PromoApp /></StrictMode>,
);
