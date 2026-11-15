import { SCENES } from "../storyboard";
import { RealFeatureShot } from "../real-ui/RealFeatureShot";
import styles from "./Scenes.module.css";

export function SwapScene() {
  const scene = SCENES[5];
  return <section data-promo-scene={scene.id} className={styles.scene} aria-label={scene.headline}>
    <div className={styles.topline}><span>GF3 / ZAMIANY ZMIAN</span><span>Rzeczywisty interfejs · Dane demonstracyjne</span></div>
    <div className={styles.heading}><h1 data-promo-headline>{scene.headline}</h1><p data-promo-supporting>{scene.supporting}</p></div>
    <div className={styles.realDemo} data-promo-demo><RealFeatureShot feature="swap" /></div>
    <div className={styles.bottomline}><span>Sklep Centrum · Październik 2026</span><span>06 / 09</span></div>
  </section>;
}
