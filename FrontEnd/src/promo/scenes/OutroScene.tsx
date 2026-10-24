import { SCENES } from "../storyboard";
import styles from "./Scenes.module.css";

export function OutroScene() {
  const scene = SCENES[8];
  return <section data-promo-scene={scene.id} className={`${styles.scene} ${styles.outro}`} aria-label={scene.headline}>
    <svg className={styles.motif} viewBox="0 0 1400 650" aria-hidden="true"><path data-promo-motif d="M100 500 V120 Q100 70 150 70 H1250 Q1300 70 1300 120 V530 Q1300 580 1250 580 H150 Q100 580 100 530 M100 220 H1300 M430 220 V580 M800 220 V580" fill="none" stroke="#2563eb" strokeWidth="2" strokeDasharray="5000" /></svg>
    <h1 data-promo-headline>{scene.headline}</h1><p data-promo-supporting>{scene.supporting}</p><div className={styles.cta} data-promo-cta>Zobacz, jak działa. <span>↗</span></div>
    <div className={styles.bottomline}><span>GF3 · Planowanie zmian</span><span>09 / 09</span></div>
  </section>;
}
