import { SCENES } from "../storyboard";
import { RealFeatureShot } from "../real-ui/RealFeatureShot";
import styles from "./Scenes.module.css";
export function ScheduleScene() {
  const scene = SCENES[4];
  return <section data-promo-scene={scene.id} className={styles.scene} aria-label={scene.headline}>
    <div className={styles.topline}><span>GF3 / GRAFIK PRACOWNIKA</span><span>Rzeczywisty interfejs · Dane demonstracyjne</span></div>
    <div className={styles.heading}><h1 data-promo-headline>{scene.headline}</h1>{scene.supporting && <p data-promo-supporting>{scene.supporting}</p>}</div>
    <div className={styles.realDemo} data-promo-demo><RealFeatureShot feature="schedule" /></div>
    <div className={styles.bottomline}><span>Sklep Centrum · Październik 2026</span><span>05 / 09</span></div>
  </section>;
}
