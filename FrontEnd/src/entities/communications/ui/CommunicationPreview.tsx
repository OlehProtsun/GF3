import { useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { t } from "@shared/i18n";
import { CloseIcon } from "@shared/ui/icons";
import { CommunicationMessageContent, CommunicationMessageFooter } from "./CommunicationMessageContent";
import styles from "./EmployeeCommunicationDialog.module.css";

export function CommunicationPreview({ message, blocks, onClose }: {
  message: Parameters<typeof CommunicationMessageContent>[0]["message"];
  blocks: Parameters<typeof CommunicationMessageContent>[0]["blocks"];
  onClose: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const [checked, setChecked] = useState(false);
  useLayoutEffect(() => {
    const dialog = ref.current;
    dialog?.showModal();
    return () => { if (dialog?.open) dialog.close(); };
  }, []);

  return createPortal(
    <dialog className={styles.preview} ref={ref} aria-label={t("Employee Preview")} onClose={event => {
      if (!event.currentTarget.open) onClose();
    }}>
      <div className={styles.previewBar}>
        <strong>{t("Employee Preview")}</strong>
        <button type="button" className={styles.iconButton} aria-label={t("Close")} onClick={() => ref.current?.close()}>
          <CloseIcon size={16} />
        </button>
      </div>
      <section className={styles.dialog}>
        <div className={styles.slide}>
          <CommunicationMessageContent message={message} blocks={blocks} onClose={() => ref.current?.close()} />
        </div>
        <CommunicationMessageFooter doNotShowAgain={checked} onChange={setChecked} onClose={() => ref.current?.close()} />
      </section>
    </dialog>,
    document.body,
  );
}
