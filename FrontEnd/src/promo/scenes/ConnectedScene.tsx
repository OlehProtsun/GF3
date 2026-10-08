import { SCENES } from "../storyboard";
import { RealFeatureShot } from "../real-ui/RealFeatureShot";
import styles from "./Scenes.module.css";

export function ConnectedScene() {
  const scene = SCENES[7];
  return <section data-promo-scene={scene.id} className={styles.scene} aria-label={scene.headline}>
    <div className={styles.topline}><span>GF3 / WSZYSTKO SIĘ ŁĄCZY</span><span>Jeden zespół. Wspólny obraz.</span></div>
    <div className={styles.heading}><h1 data-promo-headline>{scene.headline}</h1></div>
    <div className={styles.connectedWindows} data-promo-demo><div className={styles.miniWindow} data-promo-window="manager"><div className={styles.miniContent}><RealFeatureShot feature="manager" /></div><span>Plan zespołu</span></div><div className={styles.miniWindow} data-promo-window="employee"><div className={styles.miniContent}><RealFeatureShot feature="schedule" /></div><span>Moje zmiany</span></div><div className={styles.miniWindow} data-promo-window="availability"><div className={styles.miniContent}><RealFeatureShot feature="availability" /></div><span>Dostępność</span></div><div className={styles.miniWindow} data-promo-window="swap"><div className={styles.miniContent}><RealFeatureShot feature="swap" /></div><span>Zamiany</span></div></div>
    <svg className={styles.connectionLines} viewBox="0 0 1500 40" aria-hidden="true"><path data-promo-connections d="M250 20 H1250" fill="none" stroke="#2563eb" strokeWidth="3" strokeDasharray="1000" /></svg>
    <div className={styles.overview} data-promo-overview><strong>GF3</strong><div>Grafik <span>→</span> Dostępność <span>→</span> Zmiany <span>→</span> Zamiany</div><p>Wspólny plan. Czytelny dzień.</p></div>
    <div className={styles.bottomline}><span>Planowanie, które łączy zespół</span><span>08 / 09</span></div>
  </section>;
}
