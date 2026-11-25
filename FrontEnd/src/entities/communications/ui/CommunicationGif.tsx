import { t } from "@shared/i18n";
import { useEffect, useRef, useState } from "react";
import { communicationsApi } from "../api/communicationsApi";
import styles from "./CommunicationGif.module.css";

function MediaImage({ id, attachmentKey, file, photo = false, thumbnail = false }: MediaSource & { photo?: boolean; thumbnail?: boolean }) {
  const [url, setUrl] = useState<string | null>(null);
  const imageUrl = url;
  const enlarged = useRef<HTMLDialogElement>(null);
  const [loadedUrl, setLoadedUrl] = useState<string>();
  const loaded = imageUrl !== null && loadedUrl === imageUrl;
  const [failedSource, setFailedSource] = useState<{ file?: File; key?: string }>();
  const failed = failedSource !== undefined && failedSource.file === file && failedSource.key === attachmentKey;

  useEffect(() => {
    if (file) {
      const objectUrl = URL.createObjectURL(file);
      setUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }
    if (id === undefined || !attachmentKey) return;
    const controller = new AbortController();
    let objectUrl: string | undefined;
    void communicationsApi.gif(id, attachmentKey, controller.signal, photo).then(({ blob }) => {
      if (controller.signal.aborted) return;
      objectUrl = URL.createObjectURL(blob);
      setUrl(objectUrl);
    }).catch(() => {
      if (!controller.signal.aborted) setFailedSource({ file, key: attachmentKey });
    });
    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [attachmentKey, file, id, photo]);

  if (failed) return <p role="status">{t(photo ? "The photo could not be loaded." : "The animation could not be loaded.")}</p>;
  const image = imageUrl && (
    <img src={imageUrl} alt={t(photo ? "Photo attachment" : "Animated demonstration")}
      className={styles.image} hidden={!loaded} onLoad={() => setLoadedUrl(imageUrl)} onError={() => setFailedSource({ file, key: attachmentKey })} />
  );
  return <>
    {!loaded && <p role="status">{t(photo ? "Loading photo…" : "Loading animation…")}</p>}
    {photo && !thumbnail && imageUrl ? (
      <>
        <button type="button" className={styles.photoButton} hidden={!loaded}
          aria-label={t("Enlarge photo")} onClick={() => enlarged.current?.showModal()}>
          {image}
        </button>
        <dialog ref={enlarged} className={styles.enlarged} aria-label={t("Photo attachment")}>
          <button type="button" className={styles.toggle} onClick={() => enlarged.current?.close()}>{t("Close")}</button>
          <img src={imageUrl} alt={t("Photo attachment")} />
        </dialog>
      </>
    ) : image}
  </>;
}

type MediaSource = { id?: number; attachmentKey?: string; file?: File };

export function CommunicationPhoto(props: MediaSource & { thumbnail?: boolean }) {
  return <div className={styles.frame} data-employee={!props.thumbnail || undefined}><MediaImage key={`${props.id}:${props.attachmentKey}`} {...props} photo /></div>;
}

export function CommunicationGif({ employee = false, ...source }: MediaSource & { employee?: boolean; thumbnail?: boolean }) {
  const [playing, setPlaying] = useState(() =>
    !employee || !window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches);

  return <div className={styles.frame} data-employee={employee || undefined}>
    {employee && (
      <button type="button" className={styles.toggle} onClick={() => setPlaying(value => !value)}>
        {t(playing ? "Hide animation" : "Show animation")}
      </button>
    )}
    {playing && <MediaImage key={`${source.id}:${source.attachmentKey}`} {...source} />}
  </div>;
}
