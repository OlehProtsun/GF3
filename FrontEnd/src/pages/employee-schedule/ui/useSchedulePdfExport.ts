import { useEffect, useRef, useState } from "react";
import { getErrorMessage, isRequestCanceledError, type DownloadedFile } from "@shared/api/httpClient";
import { t } from "@shared/i18n";
import { downloadExportFile } from "@entities/exports/lib/download";

export function useSchedulePdfExport(
  scheduleId: number | undefined,
  loadPdf: (scheduleId: number, signal?: AbortSignal) => Promise<DownloadedFile>,
  employeeId?: number | null,
  userName?: string | null,
) {
  const [pdfExportError, setPdfExportError] = useState<string | null>(null);
  const [isPdfLoading, setIsPdfLoading] = useState(false);
  const [isPdfPreparing, setIsPdfPreparing] = useState(false);
  const [pdfFile, setPdfFile] = useState<DownloadedFile | null>(null);
  const pdfRequestAbortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    setIsPdfLoading(false);
    setIsPdfPreparing(false);
    setPdfFile(null);
    setPdfExportError(null);
    return () => {
      pdfRequestAbortRef.current?.abort();
      pdfRequestAbortRef.current = null;
    };
  }, [scheduleId, employeeId, userName, loadPdf]);

  const handleExportPdf = async () => {
    if (scheduleId === undefined || pdfRequestAbortRef.current) {
      return;
    }

    setPdfExportError(null);
    const controller = new AbortController();
    pdfRequestAbortRef.current = controller;
    setIsPdfLoading(true);
    setIsPdfPreparing(!pdfFile);
    try {
      const file = pdfFile ?? await loadPdf(scheduleId, controller.signal);
      if (controller.signal.aborted || pdfRequestAbortRef.current !== controller) return;
      if (file.blob.size === 0 || !(file.contentType ?? file.blob.type).toLowerCase().includes("application/pdf")) {
        throw new Error(t("Could not export this schedule to PDF."));
      }
      const preparedFile = { ...file, fileName: file.fileName?.trim() || `GF3_Graph_${scheduleId}.pdf` };
      setIsPdfPreparing(false);
      const shareFile = new File([file.blob], preparedFile.fileName, { type: "application/pdf" });
      if (typeof navigator.share === "function" && navigator.canShare?.({ files: [shareFile] })) {
        try {
          await navigator.share({ files: [shareFile] });
        } catch (error) {
          if (controller.signal.aborted) return;
          const name = typeof error === "object" && error !== null && "name" in error ? error.name : "";
          if (name === "NotAllowedError") {
            // A slow export can consume Safari's activation window; reuse the file on the next direct tap.
            setPdfFile(preparedFile);
            return;
          }
          if (name !== "AbortError") throw error;
        }
      } else {
        downloadExportFile(preparedFile, preparedFile.fileName);
      }
      if (!controller.signal.aborted) setPdfFile(null);
    } catch (error) {
      if (!controller.signal.aborted && !isRequestCanceledError(error))
        setPdfExportError(getErrorMessage(error, t("Could not export this schedule to PDF.")));
    } finally {
      if (pdfRequestAbortRef.current === controller) {
        pdfRequestAbortRef.current = null;
        setIsPdfLoading(false);
        setIsPdfPreparing(false);
      }
    }
  };

  return { pdfExportError, isPdfLoading, isPdfPreparing, pdfFile, setPdfFile, handleExportPdf };
}
