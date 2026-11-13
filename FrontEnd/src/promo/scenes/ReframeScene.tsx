import { SCENES } from "../storyboard";
import { RealFeatureShot } from "../real-ui/RealFeatureShot";
import styles from "./Scenes.module.css";

export function ReframeScene() {
  const scene = SCENES[1];
  return <section data-promo-scene={scene.id} className={styles.scene} aria-label={scene.headline}>
    <div className={styles.topline}><span>GF3</span><span>Jedno miejsce dla zespołu</span></div>
    <div className={styles.heading}><h1 data-promo-headline>{scene.headline}</h1></div>
    <div className={styles.reframeCard} data-promo-workspace><span className={styles.workspaceLabel}>TWÓJ ZESPÓŁ. TWÓJ PLAN.</span><strong data-promo-reveal-brand>GF3</strong><p data-promo-supporting>{scene.supporting}</p><div className={styles.revealShot}><RealFeatureShot feature="manager" /></div></div>
    <div className={styles.bottomline}><span>Planowanie zmian w jednym miejscu</span><span>02 / 09</span></div>
  </section>;
}
